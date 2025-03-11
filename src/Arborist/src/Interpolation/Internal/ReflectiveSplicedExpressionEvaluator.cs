using System.Collections.ObjectModel;
using System.Reflection;

namespace Arborist.Interpolation.Internal;

internal sealed class ReflectiveSplicedExpressionEvaluator {
    private static ImmutableDictionary<(Type, Type), Func<object?, object?>> CastHelperCache =
        ImmutableDictionary<(Type, Type), Func<object?, object?>>.Empty;

    private static bool TryGetCastHelper(
        Type targetType,
        Type sourceType,
        [NotNullWhen(true)] out Func<object?, object?>? helper
    ) {
        var cacheKey = (targetType, sourceType);
        if(CastHelperCache.TryGetValue(cacheKey, out helper))
            return true;
        if(!IsSupportedCast(targetType, sourceType))
            return false;

        helper = CreateCastHelper(targetType, sourceType);
        CastHelperCache = CastHelperCache.SetItem(cacheKey, helper);
        return true;
    }

    private static Func<object?, object?> CreateCastHelper(Type targetType, Type sourceType) {
        var parameter = Expression.Parameter(typeof(object));

        return Expression.Lambda<Func<object?, object?>>(
            Expression.Convert(
                Expression.Convert(
                    Expression.Convert(parameter, sourceType),
                    targetType
                ),
                typeof(object)
            ),
            parameter
        )
        .Compile();
    }

    private static bool IsSupportedCast(Type targetType, Type sourceType) =>
        targetType == typeof(object)
        || sourceType == typeof(object)
        || IsPrimitiveCastType(sourceType) && IsPrimitiveCastType(targetType);

    private static bool IsPrimitiveCastType(Type type) =>
        type.IsAssignableTo(typeof(System.IConvertible))
        || Nullable.GetUnderlyingType(type) is {} underlying && IsPrimitiveCastType(underlying);

    public static ReflectiveSplicedExpressionEvaluator Instance { get; } = new();

    public bool TryEvaluate<TData>(TData data, Expression expression, out object? value) {
        // This is a pain in the butt because you can't start evaluation until you are sure you can
        // evaluate the entire tree, so we do an initial pass disabling evaluation to check that
        // this is possible, and then a second pass to actually perform the evaluation.
        var verificationContext = new EvaluationContext<TData, Expression>(
            data: data,
            input: expression,
            evaluate: false
        );

        if(!TryEvaluate(verificationContext, out _)) {
            value = default;
            return false;
        }

        var evaluationContext = new EvaluationContext<TData, Expression>(
            data: data,
            input: expression,
            evaluate: true
        );

        if(!TryEvaluate(evaluationContext, out value))
            throw new Exception();

        return true;
    }

    private readonly struct EvaluationContext<TData, TInput>(
        TData data,
        bool evaluate,
        TInput input
    ) {
        public readonly TData Data = data;
        public readonly bool Evaluate = evaluate;
        public readonly TInput Input = input;

        public EvaluationContext<TData, T> WithInput<T>(T input) =>
            new(Data, Evaluate, input);
    }

    private bool TryEvaluate<TData>(EvaluationContext<TData, Expression> context, out object? value) {
        switch(context.Input) {
            case ConstantExpression:
                value = ((ConstantExpression)context.Input).Value;
                return true;

            case ListInitExpression:
                return TryEvaluateListInit(context.WithInput((ListInitExpression)context.Input), out value);

            case MemberExpression:
                return TryEvaluateMember(context.WithInput((MemberExpression)context.Input), out value);

            case MethodCallExpression:
                return TryEvaluateMethodCall(context.WithInput((MethodCallExpression)context.Input), out value);

            case NewExpression:
                return TryEvaluateNew(context.WithInput((NewExpression)context.Input), out value);

            case UnaryExpression:
                return TryEvaluateUnary(context.WithInput((UnaryExpression)context.Input), out value);

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateMany<TData>(
        EvaluationContext<TData, ReadOnlyCollection<Expression>> context,
        [NotNullWhen(true)] out object?[]? values
    ) {
        values = default;
        var expressionCount = context.Input.Count;
        if(expressionCount != 0) {
            var expressionIndex = 0;
            foreach(var expression in context.Input) {
                if(!TryEvaluate(context.WithInput(expression), out var value))
                    return false;

                if(context.Evaluate) {
                    values ??= new object?[expressionCount];
                    values[expressionIndex] = value;
                }

                expressionIndex += 1;
            }
        }

        // If the values array was not initialized by this point, either we are not evaluating or
        // there are no input expressions. Either way we can provide the empty array as a result.
        values ??= Array.Empty<object?>();
        return true;
    }

    private bool TryEvaluateListInit<TData>(EvaluationContext<TData, ListInitExpression> context, out object? value) =>
        TryEvaluateNew(context.WithInput(context.Input.NewExpression), out value)
        && TryEvaluateElementInits(value, context.WithInput(context.Input.Initializers));

    private bool TryEvaluateElementInits<TData>(object? baseValue, EvaluationContext<TData, ReadOnlyCollection<ElementInit>> context) {
        foreach(var elementInit in context.Input) {
            if(!TryEvaluateMany(context.WithInput(elementInit.Arguments), out var argValues))
                return false;

            if(context.Evaluate)
                elementInit.AddMethod.Invoke(baseValue, argValues);
        }

        return true;
    }

    private bool TryEvaluateMember<TData>(EvaluationContext<TData, MemberExpression> context, out object? value) {
        switch(context.Input) {
            // Interpolation data access
            case { Expression: {}, Member: PropertyInfo { Name: nameof(IInterpolationContext<TData>.Data) } }
                when context.Input.Member == typeof(IInterpolationContext<TData>).GetProperty(nameof(IInterpolationContext<TData>.Data)):
                value = context.Evaluate ? context.Data : default;
                return true;

            case { Member: FieldInfo field, Expression: not null }
                when TryEvaluate(context.WithInput(context.Input.Expression), out var baseValue):
                value = context.Evaluate ? field.GetValue(baseValue) : default;
                return true;

            case { Member: FieldInfo field, Expression: null }:
                value = context.Evaluate ? field.GetValue(null) : default;
                return true;

            case { Member: PropertyInfo property, Expression: not null }
                when TryEvaluate(context.WithInput(context.Input.Expression), out var baseValue):
                value = context.Evaluate ? property.GetValue(baseValue) : default;
                return true;

            case { Member: PropertyInfo property, Expression: null }:
                value = context.Evaluate ? property.GetValue(null) : default;
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateMethodCall<TData>(EvaluationContext<TData, MethodCallExpression> context, out object? value) {
        switch(context.Input) {
            case { Object: not null }
                when TryEvaluate(context.WithInput(context.Input.Object), out var baseValue)
                && TryEvaluateMany(context.WithInput(context.Input.Arguments), out var argValues):
                value = context.Evaluate ? context.Input.Method.Invoke(baseValue, argValues) : default;
                return true;

            case { Object: null }
                when TryEvaluateMany(context.WithInput(context.Input.Arguments), out var argValues):
                value = context.Evaluate ? context.Input.Method.Invoke(null, argValues) : default;
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateNew<TData>(EvaluationContext<TData, NewExpression> context, out object? value) {
        switch(context.Input) {
            case { Constructor: not null }
                when TryEvaluateMany(context.WithInput(context.Input.Arguments), out var argValues):
                value = context.Evaluate ? context.Input.Constructor.Invoke(argValues) : default;
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateUnary<TData>(EvaluationContext<TData, UnaryExpression> context, out object? value) {
        switch(context.Input.NodeType) {
            case ExpressionType.Convert:
                return TryEvaluateConvert(context, out value);

            case ExpressionType.Quote:
                value = context.Input.Operand;
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateConvert<TData>(EvaluationContext<TData, UnaryExpression> context, out object? value) {
        if(TryEvaluate(context.WithInput(context.Input.Operand), out var baseValue)) {
            var fromType = context.Input.Operand.Type;
            var targetType = context.Input.Type;

            if(context.Input.Method is not null) {
                value = context.Evaluate ? context.Input.Method.Invoke(null, [baseValue]) : default;
                return true;
            }
            if(fromType.IsAssignableTo(targetType)) {
                value = baseValue;
                return true;
            }
            if(TryGetCastHelper(targetType, fromType, out var castHelper)) {
                value = context.Evaluate ? castHelper(baseValue) : default;
                return true;
            }
        }

        value = default;
        return false;
    }

}
