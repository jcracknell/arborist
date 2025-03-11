using System.Reflection;

namespace Arborist.Interpolation.Internal;

internal sealed class ReflectiveSplicedExpressionEvaluator {
    public static ReflectiveSplicedExpressionEvaluator Instance { get; } = new();

    public bool TryEvaluate<TData>(TData data, Expression expression, out object? value) {
        switch(expression) {
            case ConstantExpression:
                value = ((ConstantExpression)expression).Value;
                return true;

            case MemberExpression:
                return TryEvaluateMember(data, (MemberExpression)expression, out value);

            case MethodCallExpression:
                return TryEvaluateMethodCall(data, (MethodCallExpression)expression, out value);

            case NewExpression:
                return TryEvaluateNew(data, (NewExpression)expression, out value);

            case UnaryExpression:
                return TryEvaluateUnary(data, (UnaryExpression)expression, out value);

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateMany<TData>(
        TData data,
        IReadOnlyCollection<Expression> expressions,
        [NotNullWhen(true)] out object?[]? values
    ) {
        values = default;
        var count = expressions.Count;
        if(count != 0) {
            var expressionIndex = 0;
            foreach(var expression in expressions) {
                if(!TryEvaluate(data, expression, out var value))
                    return false;

                values ??= new object?[count];
                values[expressionIndex] = value;
                expressionIndex += 1;
            }
        }

        values ??= Array.Empty<object?>();
        return true;
    }

    private bool TryEvaluateMember<TData>(TData data, MemberExpression expression, out object? value) {
        switch(expression) {
            // Interpolation data access
            case { Expression: {}, Member: PropertyInfo { Name: nameof(IInterpolationContext<TData>.Data) } }
                when expression.Member == typeof(IInterpolationContext<TData>).GetProperty(nameof(IInterpolationContext<TData>.Data)):
                value = data;
                return true;

            case { Member: FieldInfo field, Expression: not null }
                when TryEvaluate(data, expression.Expression, out var baseValue):
                value = field.GetValue(baseValue);
                return true;

            case { Member: FieldInfo field, Expression: null }:
                value = field.GetValue(null);
                return true;

            case { Member: PropertyInfo property, Expression: not null }
                when TryEvaluate(data, expression.Expression, out var baseValue):
                value = property.GetValue(baseValue);
                return true;

            case { Member: PropertyInfo property, Expression: null }:
                value = property.GetValue(null);
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateMethodCall<TData>(TData data, MethodCallExpression expression, out object? value) {
        switch(expression) {
            case { Object: not null }
                when TryEvaluate(data, expression.Object, out var baseValue)
                && TryEvaluateMany(data, expression.Arguments, out var argValues):
                value = expression.Method.Invoke(baseValue, argValues);
                return true;

            case { Object: null }
                when TryEvaluateMany(data, expression.Arguments, out var argValues):
                value = expression.Method.Invoke(null, argValues);
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateNew<TData>(TData data, NewExpression expression, out object? value) {
        switch(expression) {
            case { Constructor: not null }
                when TryEvaluateMany(data, expression.Arguments, out var argValues):
                value = expression.Constructor.Invoke(argValues);
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateUnary<TData>(TData data, UnaryExpression expression, out object? value) {
        switch(expression.NodeType) {
            case ExpressionType.Convert:
                return TryEvaluateConvert(data, expression, out value);

            case ExpressionType.Quote:
                value = expression.Operand;
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateConvert<TData>(TData data, UnaryExpression expression, out object? value) {
        if(TryEvaluate(data, expression.Operand, out var baseValue)) {
            if(expression.Method is not null) {
                value = expression.Method.Invoke(null, [baseValue]);
                return true;
            }

            if(baseValue is null && (!expression.Type.IsValueType || Nullable.GetUnderlyingType(expression.Type) is not null)) {
                value = baseValue;
                return true;
            }

            if(baseValue?.GetType().IsAssignableTo(expression.Type) is true) {
                value = baseValue;
                return true;
            }

            try {
                value = Convert.ChangeType(baseValue, expression.Type);
                return true;
            } catch {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }
}
