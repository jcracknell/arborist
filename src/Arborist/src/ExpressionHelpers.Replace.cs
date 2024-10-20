using Arborist.Internal;
using System.Collections.Immutable;

namespace Arborist;

public static partial class ExpressionHelpers {
    /// <summary>
    /// Replaces all occurrences of the provided <paramref name="search"/> expression with the
    /// <paramref name="replacement"/> expression in the subject <paramref name="expression"/>.
    /// </summary>
    public static Expression Replace(Expression expression, Expression search, Expression replacement) =>
        // TODO: Could potentially add an optimized single-element IReadOnlyDictionary implementation for this case
        Replace(expression, ImmutableDictionary<Expression, Expression>.Empty.Add(search, replacement));

    /// <summary>
    /// Replaces all occurrences of the expressions identified by the provided <paramref name="replacements"/> mapping
    /// in the subject <paramref name="expression"/>.
    /// </summary>
    public static Expression Replace(Expression expression, IEnumerable<KeyValuePair<Expression, Expression>> replacements) =>
        Replace(expression, replacements switch {
            IReadOnlyDictionary<Expression, Expression> dictionary => dictionary,
            _ => CreateReplacementsDictionary(replacements)
        });

    /// <summary>
    /// Replaces all occurrences of the expressions identified by the provided <paramref name="replacements"/> mapping
    /// in the subject <paramref name="expression"/>.
    /// </summary>
    public static Expression Replace(Expression expression, IReadOnlyDictionary<Expression, Expression> replacements) =>
        new ReplacingExpressionVisitor(replacements).Visit(expression);

    private static IReadOnlyDictionary<Expression, Expression> CreateReplacementsDictionary(
        IEnumerable<KeyValuePair<Expression, Expression>> replacements
    ) {
        // If you are manually stitching together a large number of expression trees, it is common to end up with
        // duplicated ParameterExpression instances, so we have to build up the replacements dictionary via assignment.
        var count = replacements.TryGetNonEnumeratedCount(out var n) ? n : 0;
        var dictionary = new Dictionary<Expression, Expression>(0);
        foreach(var (search, replace) in replacements) {
            if(dictionary.TryGetValue(search, out var existing) && !replace.Equals(existing))
                throw new InvalidOperationException($"Provided collection of replacement expressions contains multiple replacements for search expression {search}.");

            dictionary[search] = replace;
        }

        return dictionary;
    }
}
