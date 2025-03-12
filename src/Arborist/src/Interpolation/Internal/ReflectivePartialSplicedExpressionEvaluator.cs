using System.Collections.ObjectModel;
using System.Reflection;

namespace Arborist.Interpolation.Internal;

/// <summary>
/// Reflection-based <see cref="IPartialSplicedExpressionEvaluator"/> implementation.
/// </summary>
public class ReflectivePartialSplicedExpressionEvaluator : IPartialSplicedExpressionEvaluator {
    public static ReflectivePartialSplicedExpressionEvaluator Instance { get; } = new();

    public bool TryEvaluate<TData>(TData data, Expression expression, out object? value) {
        var context = new PartialSplicedExpressionEvaluationContext<TData>(
            data: data,
            dataReferences: ImmutableHashSet<Expression>.Empty,
            expression: expression
        );

        return TryEvaluate(context, out value);
    }

    public bool TryEvaluate<TData>(PartialSplicedExpressionEvaluationContext<TData> context, out object? value) {
        // This is a pain in the butt because you can't start evaluation until you are sure you can
        // evaluate the entire tree, so we do an initial pass disabling evaluation to check that
        // this is possible, and then a second pass to actually perform the evaluation.
        var verificationContext = new EvaluationContext<TData, Expression>(
            data: context.Data,
            evaluate: false,
            dataReferences: context.DataReferences,
            input: context.Expression
        );

        if(!TryEvaluate(verificationContext, out value))
            return false;

        var evaluationContext = new EvaluationContext<TData, Expression>(
            data: context.Data,
            evaluate: true,
            dataReferences: context.DataReferences,
            input: context.Expression
        );

        if(!TryEvaluate(evaluationContext, out value))
            throw new Exception($"Evaluation of expression `{context.Expression}` failed after successful verification?");

        return true;
    }

    private readonly struct EvaluationContext<TData, TInput>(
        TData data,
        IReadOnlySet<Expression> dataReferences,
        bool evaluate,
        TInput input
    ) {
        public readonly TData Data = data;
        public readonly IReadOnlySet<Expression> DataReferences = dataReferences;
        public readonly bool Evaluate = evaluate;
        public readonly TInput Input = input;

        public EvaluationContext<TData, T> WithInput<T>(T input) =>
            new(
                data: Data,
                dataReferences: DataReferences,
                evaluate: Evaluate,
                input: input
            );
    }

    private bool TryEvaluate<TData>(EvaluationContext<TData, Expression> context, out object? value) {
        switch(context.Input) {
            case {} when context.DataReferences.Contains(context.Input):
                value = context.Evaluate ? context.Data : default;
                return true;

            // This is syntactic sugar for a cascading if statement, and should be ordered by prevalence
            case MemberExpression:
                return TryEvaluateMember(context.WithInput((MemberExpression)context.Input), out value);

            case MethodCallExpression:
                return TryEvaluateMethodCall(context.WithInput((MethodCallExpression)context.Input), out value);

            case ConstantExpression:
                value = ((ConstantExpression)context.Input).Value;
                return true;

            case UnaryExpression:
                return TryEvaluateUnary(context.WithInput((UnaryExpression)context.Input), out value);

            case NewExpression:
                return TryEvaluateNew(context.WithInput((NewExpression)context.Input), out value);

            case IndexExpression:
                return TryEvaluateIndex(context.WithInput((IndexExpression)context.Input), out value);

            case BinaryExpression:
                return TryEvaluateBinary(context.WithInput((BinaryExpression)context.Input), out value);

            case MemberInitExpression:
                return TryEvaluateMemberInit(context.WithInput((MemberInitExpression)context.Input), out value);

            case ListInitExpression:
                return TryEvaluateListInit(context.WithInput((ListInitExpression)context.Input), out value);

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateMany<TData>(
        EvaluationContext<TData, ReadOnlyCollection<Expression>> context,
        out object?[]? values
    ) {
        values = default;
        var expressionCount = context.Input.Count;
        if(expressionCount == 0)
            return true;

        for(var i = 0; i < expressionCount; i++) {
            if(!TryEvaluate(context.WithInput(context.Input[i]), out var value))
                return false;

            if(context.Evaluate) {
                values ??= new object?[expressionCount];
                values[i] = value;
            }
        }

        return true;
    }

    private bool TryGetMember(object? baseValue, MemberInfo member, bool evaluate, out object? value) {
        switch(member) {
            case PropertyInfo property:
                value = evaluate ? property.GetValue(baseValue) : default;
                return true;

            case FieldInfo field:
                value = evaluate ? field.GetValue(baseValue) : default;
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TrySetMember(object? baseValue, MemberInfo member, object? value, bool evaluate) {
        switch(member) {
            case PropertyInfo property:
                if(evaluate)
                    property.SetValue(baseValue, value);
                return true;

            case FieldInfo field:
                if(evaluate)
                    field.SetValue(baseValue, value);
                return true;

            default:
                return false;
        }
    }

    private bool TryEvaluateBinary<TData>(EvaluationContext<TData, BinaryExpression> context, out object? value) {
        switch(context.Input.NodeType) {
            case ExpressionType.ArrayIndex
                when TryEvaluate(context.WithInput(context.Input.Left), out var baseValue)
                && TryEvaluate(context.WithInput(context.Input.Right), out var offsetValue):
                value = context.Evaluate switch {
                    false => default,
                    true =>
                        typeof(Array).GetMethod(nameof(Array.GetValue), [typeof(int)])!
                        .Invoke(baseValue, [offsetValue])
                };
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateIndex<TData>(EvaluationContext<TData, IndexExpression> context, out object? value) {
        switch(context.Input) {
            case { Indexer: not null, Object: not null }
                when TryEvaluate(context.WithInput(context.Input.Object), out var baseValue)
                && TryEvaluateMany(context.WithInput(context.Input.Arguments), out var indexerArgs):
                value = context.Evaluate ? context.Input.Indexer.GetValue(baseValue, indexerArgs) : default;
                return true;


            // Multi-dimensional array access
            case { Indexer: null, Object: not null, Type.IsArray: true }
                when TryEvaluate(context.WithInput(context.Input.Object), out var baseValue)
                && TryEvaluateMany(context.WithInput(context.Input.Arguments), out var indexerArgs):
                value = context.Evaluate switch {
                    false => default,
                    true =>
                        typeof(Array).GetMethod(nameof(Array.GetValue), [typeof(int[])])!
                        .Invoke(baseValue, [indexerArgs!.Cast<int>().ToArray()])
                };
                return true;

            default:
                value = default;
                return false;
        }
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
            // We retain this formulation of the interpolation data test to make unit testing easier
            case { Expression: {}, Member: PropertyInfo { Name: nameof(IInterpolationContext<TData>.Data) } }
                when context.Input.Member == typeof(IInterpolationContext<TData>).GetProperty(nameof(IInterpolationContext<TData>.Data)):
                value = context.Evaluate ? context.Data : default;
                return true;

            case { Expression: not null }
                when TryEvaluate(context.WithInput(context.Input.Expression), out var baseValue)
                && TryGetMember(baseValue, context.Input.Member, context.Evaluate, out value):
                return true;

            case { Expression: null }
                when TryGetMember(null, context.Input.Member, context.Evaluate, out value):
                return true;

            default:
                value = default;
                return false;
        }
    }

    private bool TryEvaluateMemberInit<TData>(EvaluationContext<TData, MemberInitExpression> context, out object? value) =>
        TryEvaluateNew(context.WithInput(context.Input.NewExpression), out value)
        && TryEvaluateMemberBinds(value, context.WithInput(context.Input.Bindings));

    private bool TryEvaluateMemberBinds<TData>(object? baseValue, EvaluationContext<TData, ReadOnlyCollection<MemberBinding>> context) {
        foreach(var binding in context.Input) {
            switch(binding) {
                case MemberAssignment assignment
                    when TryEvaluate(context.WithInput(assignment.Expression), out var assignedValue)
                    && TrySetMember(baseValue, assignment.Member, assignedValue, context.Evaluate):
                    continue;

                // Nested object initializer
                case MemberMemberBinding memberBinding
                    when TryGetMember(baseValue, memberBinding.Member, context.Evaluate, out var memberValue)
                    && TryEvaluateMemberBinds(memberValue, context.WithInput(memberBinding.Bindings)):
                    continue;

                // Nested collection initializer
                case MemberListBinding listBinding
                    when TryGetMember(baseValue, listBinding.Member, context.Evaluate, out var memberValue)
                    && TryEvaluateElementInits(memberValue, context.WithInput(listBinding.Initializers)):
                    continue;

                default:
                    return false;
            }
        }

        return true;
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

            case ExpressionType.ArrayLength
                when TryEvaluate(context.WithInput(context.Input.Operand), out var baseValue):
                value = context.Evaluate ? typeof(Array).GetProperty(nameof(Array.Length))!.GetValue(baseValue) : default;
                return true;

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
            // N.B. this handles conversions from T to Nullable<T>
            if(targetType == typeof(object) || fromType.IsAssignableTo(targetType)) {
                value = baseValue;
                return true;
            }
        }

        value = default;
        return false;
    }
}
