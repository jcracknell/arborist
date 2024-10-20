using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Arborist.Internal;

public class ReplacingExpressionVisitor : ExpressionVisitor {
    private IReadOnlyDictionary<Expression, Expression> _replacements;

    public ReplacingExpressionVisitor(IReadOnlyDictionary<Expression, Expression> replacements) {
        _replacements = replacements;
    }

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node) {
        if(node is not null && _replacements.TryGetValue(node, out var replacement))
            return replacement;

        return base.Visit(node);
    }
}
