using Arborist.Internal;
using Arborist.Interpolation;

namespace Arborist;

public static partial class ExpressionHelperExtensions {
    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [ExpressionInterpolator]
    public static Expression<Func<R>> Interpolate<R>(
        this IExpressionHelperOnNone helper,
        Expression<Func<IInterpolationContext, R>> expression
    ) =>
        InterpolateCore<object?, Func<R>>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [ExpressionInterpolator]
    public static Expression<Action> Interpolate(
        this IExpressionHelperOnNone helper,
        Expression<Action<IInterpolationContext>> expression
    ) =>
        InterpolateCore<object?, Action>(default, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [ExpressionInterpolator]
    public static Expression<Func<R>> Interpolate<TData, R>(
        this IExpressionHelperOnNone helper,
        TData data,
        Expression<Func<IInterpolationContext<TData>, R>> expression
    ) =>
        InterpolateCore<TData, Func<R>>(data, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    [ExpressionInterpolator]
    public static Expression<Action> Interpolate<TData>(
        this IExpressionHelperOnNone helper,
        TData data,
        Expression<Action<IInterpolationContext<TData>>> expression
    ) =>
        InterpolateCore<TData, Action>(data, expression);
        
    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [ExpressionInterpolator]
    public static Expression<Func<A, R>> Interpolate<A, R>(
        this IExpressionHelperOn<A> helper,
        Expression<Func<IInterpolationContext, A, R>> expression
    ) =>
        InterpolateCore<object?, Func<A, R>>(default(object), expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    [ExpressionInterpolator]
    public static Expression<Action<A>> Interpolate<A>(
        this IExpressionHelperOn<A> helper,
        Expression<Action<IInterpolationContext, A>> expression
    ) =>
        InterpolateCore<object?, Action<A>>(default(object), expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    /// <typeparam name="R">
    /// The expression result type.
    /// </typeparam>
    [ExpressionInterpolator]
    public static Expression<Func<A, R>> Interpolate<TData, A, R>(
        this IExpressionHelperOn<A> helper,
        TData data,
        Expression<Func<IInterpolationContext<TData>, A, R>> expression
    ) =>
        InterpolateCore<TData, Func<A, R>>(data, expression);

    /// <summary>
    /// Applies the interpolation process to the provided <paramref name="expression"/>, replacing
    /// calls to splicing methods defined on the provided <see cref="IInterpolationContext{TData}"/>
    /// argument with the corresponding subexpressions.
    /// </summary>
    /// <param name="data">
    /// Data provided to the interpolation process, accessible via the <see cref="IInterpolationContext{TData}.Data"/>
    /// property of the <see cref="IInterpolationContext{TData}"/> argument.
    /// </param>
    /// <typeparam name="TData">
    /// The type of the data provided to the interpolation process.
    /// </typeparam>
    [ExpressionInterpolator]
    public static Expression<Action<A>> Interpolate<TData, A>(
        this IExpressionHelperOn<A> helper,
        TData data,
        Expression<Action<IInterpolationContext<TData>, A>> expression
    ) =>
        InterpolateCore<TData, Action<A>>(data, expression);
        
    private static Expression<TDelegate> InterpolateCore<TData, TDelegate>(TData data, LambdaExpression expression)
        where TDelegate : Delegate
    {
        var analyzer = new AnalyzingInterpolationVisitor(expression);
        analyzer.Visit(expression.Body);

        var parameterExpressions = expression.Parameters.Skip(1);

        var interpolator = new SplicingInterpolationVisitor(
            evaluatedSpliceParameters: EvaluateInterpolatedExpressions(
                data: data,
                evaluatedExpressions: analyzer.EvaluatedExpressions,
                dataReferences: analyzer.DataReferences
            )
        );

        return Expression.Lambda<TDelegate>(
            body: interpolator.Visit(expression.Body),
            parameters: expression.Parameters.Skip(1)
        );
    }

    private static IReadOnlyDictionary<Expression, object?> EvaluateInterpolatedExpressions<TData>(
        TData data,
        IReadOnlySet<Expression> evaluatedExpressions,
        IReadOnlySet<MemberExpression> dataReferences
    ) {
        if(evaluatedExpressions.Count == 0)
            return ImmutableDictionary<Expression, object?>.Empty;

        var unevaluatedExpressions = default(List<Expression>);
        var evaluatedValues = new Dictionary<Expression, object?>(evaluatedExpressions.Count);
        foreach(var expr in evaluatedExpressions) {
            switch(expr) {
                case ConstantExpression { Value: var value }:
                    evaluatedValues[expr] = value;
                    break;
                case UnaryExpression { NodeType: ExpressionType.Convert, Operand: ConstantExpression { Value: var value } }:
                    evaluatedValues[expr] = value;
                    break;
                default:
                    (unevaluatedExpressions ??= new(evaluatedExpressions.Count - evaluatedValues.Count)).Add(expr);
                    break;
            }
        }

        // If there are no expressions requiring evaluation, then we can skip costly evaluation
        if(unevaluatedExpressions is not { Count: not 0 })
            return evaluatedValues;

        var dataParameter = Expression.Parameter(typeof(TData));

        // Build a dictionary mapping references to ISplicingContext.Data with the data parameter
        var dataReferenceReplacements = new Dictionary<Expression, Expression>(dataReferences.Count);
        foreach(var dataReference in dataReferences)
            dataReferenceReplacements[dataReference] = dataParameter;

        var evaluated = Expression.Lambda<Func<TData, object?[]>>(
            Expression.NewArrayInit(typeof(object),
                from expr in unevaluatedExpressions select Expression.Convert(
                    ExpressionHelper.Replace(expr, dataReferenceReplacements),
                    typeof(object)
                )
            ),
            dataParameter
        )
        .Compile()
        .Invoke(data);

        for(var i = 0; i < unevaluatedExpressions.Count; i++)
            evaluatedValues[unevaluatedExpressions[i]] = evaluated[i];

        return evaluatedValues;
    }
}
