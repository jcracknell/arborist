using Arborist.Internal.Collections;
using Arborist.Interpolation;
using Arborist.Interpolation.Internal;
using System.Reflection;

namespace Arborist;

public static partial class ExpressionHelper {
    internal static Expression<TDelegate> InterpolateCore<TData, TDelegate>(TData data, LambdaExpression expression)
        where TDelegate : Delegate
    {
        var analyzer = new AnalyzingInterpolationVisitor(expression);
        analyzer.Apply(expression.Body);

        var interpolator = new SplicingInterpolationVisitor(
            evaluatedSpliceParameters: EvaluateSplicedExpressions(
                data: data,
                expressions: analyzer.EvaluatedExpressions,
                dataReferences: analyzer.DataReferences
            )
        );

        return Expression.Lambda<TDelegate>(
            body: interpolator.Apply(expression.Body),
            parameters: expression.Parameters.Skip(1)
        );
    }

    private static IReadOnlyList<object?> EvaluateSplicedExpressions<TData>(
        TData data,
        IReadOnlyList<Expression> expressions,
        IReadOnlySet<MemberExpression> dataReferences
    ) {
        var expressionCount = expressions.Count;
        if(expressionCount == 0)
            return Array.Empty<object?>();

        var values = new object?[expressionCount];

        // First we attempt to evaluate expressions via reflection, as this is actually significantly
        // faster than compiling an Expression to a Func to achieve the same thing.
        var evaluatedCount = 0;
        while(evaluatedCount < expressionCount) {
            // In the event of a failure, we have to give up to ensure that the expressions are evaluated
            // in the expected order.
            if(!TryReflectSplicedValue(data, expressions[evaluatedCount], out var value))
                break;

            values[evaluatedCount] = value;
            evaluatedCount += 1;
        }

        if(evaluatedCount == expressionCount)
            return values;

        var compiledValues = EvaluateSplicedValuesCompiled(
            data: data,
            expressions: expressions.Skip(evaluatedCount),
            dataReferences: dataReferences
        );

        Array.Copy(compiledValues, 0, values, evaluatedCount, compiledValues.Length);

        return values;
    }

    private static object?[] EvaluateSplicedValuesCompiled<TData>(
        TData data,
        IEnumerable<Expression> expressions,
        IReadOnlySet<MemberExpression> dataReferences
    ) {
        var dataParameter = Expression.Parameter(typeof(TData));

        // Create an expression to evaluate the unevaluated expression values into an array
        return Expression.Lambda<Func<TData, object?[]>>(
            ExpressionHelper.Replace(
                Expression.NewArrayInit(typeof(object),
                    from expr in expressions
                    select Expression.Convert(expr, typeof(object))
                ),
                // Replace references to IInterpolationContext.Data with the data parameter
                SmallDictionary.CreateRange(
                    from dataReference in dataReferences
                    select new KeyValuePair<Expression, Expression>(dataReference, dataParameter)
                )
            ),
            dataParameter
        )
        .Compile()
        .Invoke(data);
    }

    private static bool TryReflectSplicedValue<TData>(TData data, Expression expression, out object? value) {
        switch(expression) {
            case ConstantExpression constant:
                value = constant.Value;
                return true;

            // Interpolation data access
            case MemberExpression { Member: PropertyInfo { Name: nameof(IInterpolationContext<TData>.Data) } } dataAccess
                when dataAccess.Expression?.Type == typeof(IInterpolationContext<TData>)
                && dataAccess.Member == typeof(IInterpolationContext<TData>).GetProperty(nameof(IInterpolationContext<TData>.Data)):
                value = data;
                return true;

            // Instance field access
            case MemberExpression { Expression: {} baseExpr, Member: FieldInfo field }
                when TryReflectSplicedValue(data, baseExpr, out var baseValue):
                value = field.GetValue(baseValue);
                return true;

            // Instance property access
            case MemberExpression { Expression: {} baseExpr, Member: PropertyInfo property }
                when TryReflectSplicedValue(data, baseExpr, out var baseValue):
                value = property.GetValue(baseValue);
                return true;

            // Instance call
            case MethodCallExpression { Object: not null } methodCall
                when TryReflectSplicedValue(data, methodCall.Object, out var objectValue)
                && TryReflectSplicedArgValues(data, methodCall.Arguments, out var argValues):
                value = methodCall.Method.Invoke(objectValue, argValues);
                return true;

            // Static field access
            case MemberExpression { Expression: null, Member: FieldInfo field }:
                value = field.GetValue(null);
                return true;

            // Static property access
            case MemberExpression { Expression: null, Member: PropertyInfo property }:
                value = property.GetValue(null);
                return true;

            // Static call
            case MethodCallExpression { Object: null } methodCall
                when TryReflectSplicedArgValues(data, methodCall.Arguments, out var argValues):
                value = methodCall.Method.Invoke(null, argValues);
                return true;

            // Type conversion
            case UnaryExpression { NodeType: ExpressionType.Convert } convert
                when TryReflectSplicedValue(data, convert.Operand, out var baseValue):
                if(convert.Method is not null) {
                    value = convert.Method.Invoke(null, [baseValue]);
                    return true;
                }

                try {
                    value = Convert.ChangeType(baseValue, convert.Type);
                    return true;
                } catch {
                    value = default;
                    return false;
                }

            default:
                value = default;
                return false;
        }
    }

    private static bool TryReflectSplicedArgValues<TData>(
        TData data,
        IReadOnlyCollection<Expression> expressions,
        [NotNullWhen(true)] out object?[]? values
    ) {
        values = default;
        var count = expressions.Count;
        if(count != 0) {
            var expressionIndex = 0;
            foreach(var expression in expressions) {
                if(!TryReflectSplicedValue(data, expression, out var value))
                    return false;

                values ??= new object?[count];
                values[expressionIndex] = value;
                expressionIndex += 1;
            }
        }

        values ??= Array.Empty<object?>();
        return true;
    }
}
