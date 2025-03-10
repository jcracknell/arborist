using Arborist.Internal;
using Arborist.Internal.Collections;

namespace Arborist;

public static partial class ExpressionHelper {
    /// <summary>
    /// Replaces all occurrences of the provided <paramref name="search"/> expression with the
    /// <paramref name="replacement"/> expression in the subject <paramref name="expression"/>.
    /// </summary>
    public static Expression Replace(Expression expression, Expression search, Expression replacement) =>
        Replace(expression, SmallDictionary.Create(KeyValuePair.Create(search, replacement)));

    /// <summary>
    /// Replaces all occurrences of the expressions identified by the provided <paramref name="replacements"/> mapping
    /// in the subject <paramref name="expression"/>.
    /// </summary>
    public static Expression Replace(Expression expression, IReadOnlyDictionary<Expression, Expression> replacements) =>
        replacements.Count switch {
            0 => expression,
            _ => new ReplacingExpressionVisitor(replacements).Visit(expression)
        };
}
