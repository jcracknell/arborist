using System.Text;

namespace Arborist.CodeGen;

public abstract class InterpolatedTree : IEquatable<InterpolatedTree> {
    public static InterpolatedTree Unsupported { get; } = new UnsupportedNode();

    /// <summary>
    /// Singleton empty <see cref="InterpolatedTree"/> value.
    /// </summary>
    public static InterpolatedTree Empty { get; } = new VerbatimNode("");

    public static InterpolatedTree Verbatim(string value) =>
        value.Length == 0 ? Empty : new VerbatimNode(value);

    public static InterpolatedTree AnonymousClass(IReadOnlyList<InterpolatedTree> propertyInitializers) =>
        Concat(Verbatim("new "), Initializer(propertyInitializers));

    public static InterpolatedTree ArrowBody(InterpolatedTree expression) =>
        new ArrowBodyNode(expression);

    public static InterpolatedTree Binary(
        string @operator,
        InterpolatedTree left,
        InterpolatedTree right
    ) =>
        new BinaryNode(@operator, left, right);

    public static InterpolatedTree Concat(params InterpolatedTree[] nodes) =>
        new ConcatNode(nodes);

    public static InterpolatedTree Concat(IReadOnlyList<InterpolatedTree> nodes) =>
        new ConcatNode(nodes);

    public static InterpolatedTree Indexer(
        InterpolatedTree body,
        InterpolatedTree index
    ) =>
        new ConcatNode(body, Verbatim("["), index, Verbatim("]"));

    public static InterpolatedTree Initializer(IReadOnlyList<InterpolatedTree> elements) =>
        new InitializerNode(elements);

    public static InterpolatedTree InstanceCall(
        InterpolatedTree expression,
        InterpolatedTree method,
        IReadOnlyList<InterpolatedTree> args
    ) =>
        new InstanceCallNode(expression, method, args);

    public static InterpolatedTree Lambda(
        IReadOnlyList<InterpolatedTree> parameters,
        InterpolatedTree body
    ) =>
        new LambdaNode(parameters, body);

    public static InterpolatedTree Member(
        InterpolatedTree expression,
        InterpolatedTree member
    ) =>
        Concat(expression, Verbatim("."), member);

    public static InterpolatedTree MethodDefinition(
        string method,
        IReadOnlyList<InterpolatedTree> parameters,
        IReadOnlyList<InterpolatedTree> typeConstraints,
        InterpolatedTree body
    ) =>
        new MethodDefinitionNode(method, parameters, typeConstraints, body);

    public static InterpolatedTree Placeholder(string identifier) =>
        new PlaceholderNode(identifier);

    public static InterpolatedTree StaticCall(
        InterpolatedTree method,
        IReadOnlyList<InterpolatedTree> args
    ) =>
        new StaticCallNode(method, args);

    public static InterpolatedTree Switch(
        InterpolatedTree subject,
        IReadOnlyList<InterpolatedTree> cases
    ) =>
        new SwitchNode(subject, cases);

    public static InterpolatedTree SwitchCase(
        InterpolatedTree pattern,
        InterpolatedTree body
    ) =>
        new SwitchCaseNode(pattern, body);

    public static InterpolatedTree Ternary(
        InterpolatedTree condition,
        InterpolatedTree thenNode,
        InterpolatedTree elseNode
    ) =>
        new TernaryNode(condition, thenNode, elseNode);

    private InterpolatedTree() { }

    public abstract bool IsSupported { get; }
    public abstract void Render(RenderingContext context);

    protected abstract InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer);

    public InterpolatedTree Replace(InterpolatedTree search, InterpolatedTree replacement) {
        return Replacer(this);

        InterpolatedTree Replacer(InterpolatedTree node) =>
            ReplaceImpl(node, search, replacement, Replacer);

        static InterpolatedTree ReplaceImpl(InterpolatedTree node, InterpolatedTree search, InterpolatedTree replacement, Func<InterpolatedTree, InterpolatedTree> replacer) =>
            node.Equals(search) ? replacement : node.Replace(replacer);
    }

    public abstract override int GetHashCode();
    public abstract bool Equals(InterpolatedTree? obj);

    public override bool Equals(object? obj) =>
        Equals(obj as InterpolatedTree);

    public override string ToString() =>
        ToString(0);

    public string ToString(int level) {
        var stringBuilder = new StringBuilder();
        Render(new RenderingContext(stringBuilder, level));
        return stringBuilder.ToString();
    }

    public readonly struct RenderingContext(StringBuilder stringBuilder, int level) {
        private const string INDENT = "    ";
        private const string NEWLINE = "\n";

        public void Append(string value) {
            stringBuilder.Append(value);
        }

        public void Append(InterpolatedTree node) {
            node.Render(this);
        }

        public void AppendIndent() {
            for(var ci = stringBuilder.Length - 1; 0 < ci; ci--) {
                var c = stringBuilder[ci];
                if(c == '\n')
                    break;
                // Skip indentation if the buffered output does not end with a newline followed by whitespace
                if(!Char.IsWhiteSpace(c))
                    return;
            }

            for(var i = 0; i < level; i++)
                stringBuilder.Append(INDENT);
        }

        public void AppendIndent(string value) {
            AppendIndent();
            Append(value);
        }

        public void Indent(InterpolatedTree node) {
            node.Render(new RenderingContext(stringBuilder, level + 1));
        }

        public void AppendNewLine() {
            stringBuilder.Append(NEWLINE);
        }
    }

    private class UnsupportedNode : InterpolatedTree {
        public override bool IsSupported => false;

        public override void Render(RenderingContext context) {
            context.AppendIndent("???");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            this;

        public override int GetHashCode() =>
            "???".GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is UnsupportedNode;
    }

    private class VerbatimNode(string expr) : InterpolatedTree {
        public string Expr { get; } = expr;

        public override bool IsSupported => true;

        public override void Render(RenderingContext context) {
            context.AppendIndent(Expr);
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            this;

        public override int GetHashCode() =>
            Expr.GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is VerbatimNode that && this.Expr.Equals(that.Expr);
    }

    private class PlaceholderNode(string identifier) : InterpolatedTree {
        public string Identifier { get; } = identifier;
        public override bool IsSupported => false;

        public override void Render(RenderingContext context) {
            context.Append($"<{Identifier}>");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            this;

        public override int GetHashCode() =>
            IdentifierEqualityComparer.Instance.GetHashCode(Identifier);

        public override bool Equals(InterpolatedTree? obj) =>
            obj is PlaceholderNode that
            && IdentifierEqualityComparer.Instance.Equals(this.Identifier, that.Identifier);
    }

    private class ArrowBodyNode(InterpolatedTree expression) : InterpolatedTree {
        public InterpolatedTree Expression { get; } = expression;

        public override bool IsSupported =>
            Expression.IsSupported;

        public override void Render(RenderingContext context) {
            context.Append(" =>");
            context.AppendNewLine();
            context.Indent(Expression);
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new ArrowBodyNode(replacer(Expression));

        public override int GetHashCode() =>
            Expression.GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is ArrowBodyNode that
            && this.Expression.Equals(that.Expression);
    }

    private class BinaryNode(string @operator, InterpolatedTree left, InterpolatedTree right)
        : InterpolatedTree
    {
        public string Operator { get; } = @operator.Trim();
        public InterpolatedTree Left { get; } = left;
        public InterpolatedTree Right { get; } = right;

        public override bool IsSupported =>
            Left.IsSupported && Right.IsSupported;

        public override void Render(RenderingContext context) {
            context.Append("(");
            context.Append(Left);
            context.Append(" ");
            context.Append(Operator);
            context.Append(" ");
            context.Append(Right);
            context.Append(")");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new BinaryNode(Operator, replacer(Left), replacer(Right));

        public override int GetHashCode() =>
            Operator.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is BinaryNode that
            && this.Left.Equals(that.Left)
            && this.Right.Equals(that.Right);
    }

    private class InstanceCallNode(
        InterpolatedTree body,
        InterpolatedTree method,
        IReadOnlyList<InterpolatedTree> args
    ) : InterpolatedTree {
        public InterpolatedTree Body { get; } = body;
        public InterpolatedTree Method { get; } = method;
        public IReadOnlyList<InterpolatedTree> Args { get; } = args;

        public override bool IsSupported =>
            Body.IsSupported && Args.All(a => a.IsSupported);

        public override void Render(RenderingContext context) {
            context.Append(Body);
            context.Append(".");
            context.Append(Method);
            context.Append("(");
            if(Args.Count != 0) {
                context.AppendNewLine();
                context.Indent(Args[0]);
                for(var i = 1; i < Args.Count; i++) {
                    context.Append(",");
                    context.AppendNewLine();
                    context.Indent(Args[i]);
                }
                context.AppendNewLine();
            }
            context.AppendIndent(")");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new InstanceCallNode(
                replacer(Body),
                replacer(Method),
                [..Args.Select(replacer)]
            );

        public override int GetHashCode() =>
            Args.Aggregate(Body.GetHashCode() ^ Method.GetHashCode(), (h, a) => h ^ a.GetHashCode());

        public override bool Equals(InterpolatedTree? obj) =>
            obj is InstanceCallNode that
            && this.Body.Equals(that.Body)
            && this.Method.Equals(that.Method)
            && this.Args.SequenceEqual(that.Args);
    }

    private class LambdaNode(IReadOnlyList<InterpolatedTree> args, InterpolatedTree body)
        : InterpolatedTree
    {
        public IReadOnlyList<InterpolatedTree> Args { get; } = args;
        public InterpolatedTree Body { get; } = body;

        public override bool IsSupported =>
            Body.IsSupported && Args.All(a => a.IsSupported);

        public override void Render(RenderingContext context) {
            context.AppendIndent("(");
            if(Args.Count != 0) {
                context.Append(Args[0]);
                for(var i = 1; i < Args.Count; i++) {
                    context.Append(", ");
                    context.Append(Args[i]);
                }
            }
            context.Append(") => ");
            context.Append(Body);
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new LambdaNode(
                [..Args.Select(replacer)],
                replacer(Body)
            );

        public override int GetHashCode() =>
            Args.Aggregate(Body.GetHashCode(), (h, a) => h ^ a.GetHashCode());

        public override bool Equals(InterpolatedTree? obj) =>
            obj is LambdaNode that
            && this.Body.Equals(that.Body)
            && this.Args.SequenceEqual(that.Args);
    }

    private class InitializerNode(IReadOnlyList<InterpolatedTree> initializers)
         : InterpolatedTree
    {
        public IReadOnlyList<InterpolatedTree> Initializers { get; } = initializers;

        public override bool IsSupported =>
            Initializers.All(static i => i.IsSupported);

        public override void Render(RenderingContext context) {
            context.AppendIndent("{");
            if(Initializers.Count != 0) {
                context.AppendNewLine();
                context.Indent(Initializers[0]);
                for(var i = 1; i < Initializers.Count; i++) {
                    context.Append(",");
                    context.AppendNewLine();
                    context.Indent(Initializers[i]);
                }
                context.AppendNewLine();
            }
            context.AppendIndent("}");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new InitializerNode([..Initializers.Select(replacer)]);

        public override int GetHashCode() =>
            Initializers.Aggregate(default(int), (h, i) => h ^ i.GetHashCode());

        public override bool Equals(InterpolatedTree? obj) =>
            obj is InitializerNode that
            && this.Initializers.SequenceEqual(that.Initializers);
    }

    private class ConcatNode(IReadOnlyList<InterpolatedTree> nodes) : InterpolatedTree {
        public IReadOnlyList<InterpolatedTree> Nodes { get; } = nodes;

        public ConcatNode(params InterpolatedTree[] nodes)
            : this((IReadOnlyList<InterpolatedTree>)nodes)
        { }

        public override bool IsSupported =>
            Nodes.All(n => n.IsSupported);

        public override void Render(RenderingContext context) {
            for(var i = 0; i < Nodes.Count; i++)
                context.Append(Nodes[i]);
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new ConcatNode([..Nodes.Select(replacer)]);

        public override int GetHashCode() =>
            Nodes.Aggregate(0, (h, n) => h ^ n.GetHashCode());

        public override bool Equals(InterpolatedTree? obj) =>
            obj is ConcatNode that
            && this.Nodes.SequenceEqual(that.Nodes);
    }

    private class MethodDefinitionNode(
        string method,
        IReadOnlyList<InterpolatedTree> parameters,
        IReadOnlyList<InterpolatedTree> typeConstraints,
        InterpolatedTree body
    ) : InterpolatedTree {
        public string Method { get; } = method;
        public IReadOnlyList<InterpolatedTree> Parameters { get; } = parameters;
        public IReadOnlyList<InterpolatedTree> TypeConstraints { get; } = typeConstraints;
        public InterpolatedTree Body { get; } = body;

        public override bool IsSupported =>
            Parameters.All(p => p.IsSupported)
            && TypeConstraints.All(c => c.IsSupported)
            && Body.IsSupported;

        public override void Render(RenderingContext context) {
            context.AppendIndent(Method);
            context.Append("(");
            if(Parameters.Count != 0) {
                context.AppendNewLine();
                context.Indent(Parameters[0]);
                for(var pi = 1; pi < Parameters.Count; pi++) {
                    context.Append(",");
                    context.AppendNewLine();
                    context.Indent(Parameters[pi]);
                }
                context.AppendNewLine();
                context.AppendIndent();
            }
            context.Append(")");
            if(TypeConstraints.Count != 0) {
                context.AppendNewLine();
                context.Indent(TypeConstraints[0]);
                for(var ci = 1; ci < TypeConstraints.Count; ci++) {
                    context.Append(",");
                    context.AppendNewLine();
                    context.Indent(TypeConstraints[ci]);
                }
                context.AppendNewLine();
            }
            context.Append(Body);
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new MethodDefinitionNode(
                Method,
                [..Parameters.Select(replacer)],
                [..TypeConstraints.Select(replacer)],
                replacer(Body)
            );

        public override int GetHashCode() {
            var hash = Method.GetHashCode();
            hash = Parameters.Aggregate(hash, (h, p) => h ^ p.GetHashCode());
            hash = TypeConstraints.Aggregate(hash, (h, c) => h ^ c.GetHashCode());
            hash ^= Body.GetHashCode();
            return hash;
        }

        public override bool Equals(InterpolatedTree? obj) =>
            obj is MethodDefinitionNode that
            && this.Method.Equals(that.Method)
            && this.Parameters.SequenceEqual(that.Parameters)
            && this.TypeConstraints.SequenceEqual(that.TypeConstraints)
            && this.Body.Equals(that.Body);
    }

    private class StaticCallNode(InterpolatedTree method, IReadOnlyList<InterpolatedTree> args) : InterpolatedTree {
        public InterpolatedTree Method { get; } = method;
        public IReadOnlyList<InterpolatedTree> Args { get; } = args;

        public override bool IsSupported =>
            Args.All(a => a.IsSupported);

        public override void Render(RenderingContext context) {
            context.Append(Method);
            context.Append("(");
            if(Args.Count != 0) {
                context.AppendNewLine();
                context.Indent(Args[0]);
                for(var i = 1; i < Args.Count; i++) {
                    context.Append(",");
                    context.AppendNewLine();
                    context.Indent(Args[i]);
                }
                context.AppendNewLine();
            }
            context.AppendIndent(")");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new StaticCallNode(replacer(Method), [..Args.Select(replacer)]);

        public override int GetHashCode() =>
            Args.Aggregate(Method.GetHashCode(), (h, a) => h ^ a.GetHashCode());

        public override bool Equals(InterpolatedTree? obj) =>
            obj is StaticCallNode that
            && this.Method.Equals(that.Method)
            && this.Args.SequenceEqual(that.Args);
    }

    private class SwitchNode(
        InterpolatedTree subject,
        IReadOnlyList<InterpolatedTree> cases
    ) : InterpolatedTree {
        public InterpolatedTree Subject { get; } = subject;
        public IReadOnlyList<InterpolatedTree> Cases { get; } = cases;

        public override bool IsSupported =>
            Subject.IsSupported && Cases.All(n => n.IsSupported);

        public override void Render(RenderingContext context) {
            context.Append(Subject);
            context.Append(" switch {");
            if(Cases.Count != 0) {
                context.AppendNewLine();
                context.Indent(Cases[0]);
                for(var i = 1; i < Cases.Count; i++) {
                    context.Append(",");
                    context.AppendNewLine();
                    context.Indent(Cases[i]);
                }
            }
            context.AppendNewLine();
            context.AppendIndent("}");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new SwitchNode(replacer(Subject), [..Cases.Select(replacer)]);

        public override int GetHashCode() =>
            Cases.Aggregate(Subject.GetHashCode(), (h, n) => h ^ n.GetHashCode());

        public override bool Equals(InterpolatedTree? obj) =>
            obj is SwitchNode that
            && this.Subject.Equals(that.Subject)
            && this.Cases.SequenceEqual(that.Cases);
    }

    private class SwitchCaseNode(
        InterpolatedTree pattern,
        InterpolatedTree body
    ) : InterpolatedTree {
        public InterpolatedTree Pattern { get; } = pattern;
        public InterpolatedTree Body { get; } = body;

        public override bool IsSupported =>
            Pattern.IsSupported && Body.IsSupported;

        public override void Render(RenderingContext context) {
            context.Append(Pattern);
            context.Append(" => ");
            context.Append(Body);
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new SwitchCaseNode(replacer(Pattern), replacer(Body));

        public override int GetHashCode() =>
            Pattern.GetHashCode() ^ Body.GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is SwitchCaseNode that
            && this.Pattern.Equals(that.Pattern)
            && this.Body.Equals(that.Body);
    }

    private class TernaryNode(InterpolatedTree condition, InterpolatedTree thenNode, InterpolatedTree elseNode)
        : InterpolatedTree
    {
        public InterpolatedTree Condition { get; } = condition;
        public InterpolatedTree ThenNode { get; } = thenNode;
        public InterpolatedTree ElseNode { get; } = elseNode;

        public override bool IsSupported =>
            Condition.IsSupported && ThenNode.IsSupported && ElseNode.IsSupported;

        public override void Render(RenderingContext context) {
            context.Append("(");
            context.Append(Condition);
            context.AppendNewLine();
            context.AppendIndent("?   ");
            context.Indent(ThenNode);
            context.AppendNewLine();
            context.AppendIndent(":   ");
            context.Indent(ElseNode);
            context.Append(")");
        }

        protected override InterpolatedTree Replace(Func<InterpolatedTree, InterpolatedTree> replacer) =>
            new TernaryNode(replacer(Condition), replacer(ThenNode), replacer(ElseNode));

        public override int GetHashCode() =>
            Condition.GetHashCode() ^ ThenNode.GetHashCode() ^ ElseNode.GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is TernaryNode that
            && this.Condition.Equals(that.Condition)
            && this.ThenNode.Equals(that.ThenNode)
            && this.ElseNode.Equals(that.ElseNode);
    }
}
