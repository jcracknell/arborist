using System.Text;

namespace Arborist.CodeGen;

public abstract class InterpolatedExpressionTree : IEquatable<InterpolatedExpressionTree> {
    public static InterpolatedExpressionTree Unsupported { get; } = new UnsupportedNode();

    public static InterpolatedExpressionTree Verbatim(string value) =>
        new VerbatimNode(value);

    public static InterpolatedExpressionTree AddInitializer(IReadOnlyList<InterpolatedExpressionTree> values) =>
        new AddInitializerNode(values);

    public static InterpolatedExpressionTree ArrowBody(InterpolatedExpressionTree expression) =>
        new ArrowBodyNode(expression);

    public static InterpolatedExpressionTree Binary(
        string @operator,
        InterpolatedExpressionTree left,
        InterpolatedExpressionTree right
    ) =>
        new BinaryNode(@operator, left, right);

    public static InterpolatedExpressionTree Concat(params InterpolatedExpressionTree[] nodes) =>
        new ConcatNode(nodes);

    public static InterpolatedExpressionTree Concat(IReadOnlyList<InterpolatedExpressionTree> nodes) =>
        new ConcatNode(nodes);

    public static InterpolatedExpressionTree Indexer(
        InterpolatedExpressionTree body,
        InterpolatedExpressionTree index
    ) =>
        new IndexerNode(body, index);

    public static InterpolatedExpressionTree InstanceCall(
        InterpolatedExpressionTree expression,
        string method,
        IReadOnlyList<InterpolatedExpressionTree> args
    ) =>
        new InstanceCallNode(expression, method, args);

    public static InterpolatedExpressionTree Lambda(
        IReadOnlyList<InterpolatedExpressionTree> parameters,
        InterpolatedExpressionTree body
    ) =>
        new LambdaNode(parameters, body);

    public static InterpolatedExpressionTree Member(
        InterpolatedExpressionTree expression,
        string member
    ) =>
        Concat(expression, Verbatim("."), Verbatim(member));

    public static InterpolatedExpressionTree MethodDefinition(
        string method,
        IReadOnlyList<InterpolatedExpressionTree> parameters,
        IReadOnlyList<InterpolatedExpressionTree> typeConstraints,
        InterpolatedExpressionTree body
    ) =>
        new MethodDefinitionNode(method, parameters, typeConstraints, body);

    public static InterpolatedExpressionTree ObjectInit(
        InterpolatedExpressionTree body,
        IReadOnlyList<InterpolatedExpressionTree> initializers
    ) =>
        new ObjectInitNode(body, initializers);

    public static InterpolatedExpressionTree StaticCall(
        string method,
        IReadOnlyList<InterpolatedExpressionTree> args
    ) =>
        new StaticCallNode(method, args);

    public static InterpolatedExpressionTree Switch(
        InterpolatedExpressionTree subject,
        IReadOnlyList<InterpolatedExpressionTree> cases
    ) =>
        new SwitchNode(subject, cases);

    public static InterpolatedExpressionTree SwitchCase(
        InterpolatedExpressionTree pattern,
        InterpolatedExpressionTree body
    ) =>
        new SwitchCaseNode(pattern, body);

    public static InterpolatedExpressionTree Ternary(
        InterpolatedExpressionTree condition,
        InterpolatedExpressionTree thenNode,
        InterpolatedExpressionTree elseNode
    ) =>
        new TernaryNode(condition, thenNode, elseNode);

    private InterpolatedExpressionTree() { }

    public abstract bool IsSupported { get; }
    public abstract void Render(RenderingContext context);
    public abstract override int GetHashCode();
    public abstract bool Equals(InterpolatedExpressionTree? obj);

    public override bool Equals(object? obj) =>
        Equals(obj as InterpolatedExpressionTree);

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

        public void Append(InterpolatedExpressionTree node) {
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

        public void Indent(InterpolatedExpressionTree node) {
            node.Render(new RenderingContext(stringBuilder, level + 1));
        }

        public void AppendNewLine() {
            stringBuilder.Append(NEWLINE);
        }
    }

    private class UnsupportedNode : InterpolatedExpressionTree {
        public override bool IsSupported => false;

        public override void Render(RenderingContext context) {
            context.AppendIndent("???");
        }

        public override int GetHashCode() =>
            "???".GetHashCode();

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is UnsupportedNode;
    }

    private class VerbatimNode(string expr) : InterpolatedExpressionTree {
        public string Expr { get; } = expr;

        public override bool IsSupported => true;

        public override void Render(RenderingContext context) {
            context.AppendIndent(Expr);
        }

        public override int GetHashCode() =>
            Expr.GetHashCode();

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is VerbatimNode that && this.Expr.Equals(that.Expr);
    }

    private class AddInitializerNode(IReadOnlyList<InterpolatedExpressionTree> values) : InterpolatedExpressionTree {
        public IReadOnlyList<InterpolatedExpressionTree> Values { get; } = values;

        public override bool IsSupported =>
            Values.All(v => v.IsSupported);

        public override void Render(RenderingContext context) {
            context.Append("{ ");
            if(Values.Count != 0) {
                context.Append(" ");
                context.Append(Values[0]);
                for(var i = 1; i < Values.Count; i++) {
                    context.Append(", ");
                    context.Append(Values[i]);
                }
            }
            context.Append(" }");
        }

        public override int GetHashCode() =>
            Values.Aggregate(default(int), (h, v) => h ^ v.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is AddInitializerNode that
            && this.Values.SequenceEqual(that.Values);
    }

    private class ArrowBodyNode(InterpolatedExpressionTree expression) : InterpolatedExpressionTree {
        public InterpolatedExpressionTree Expression { get; } = expression;

        public override bool IsSupported =>
            Expression.IsSupported;

        public override void Render(RenderingContext context) {
            context.AppendIndent();
            context.AppendNewLine();
            context.Indent(Expression);
        }

        public override int GetHashCode() =>
            Expression.GetHashCode();

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is ArrowBodyNode that
            && this.Expression.Equals(that.Expression);
    }

    private class BinaryNode(string @operator, InterpolatedExpressionTree left, InterpolatedExpressionTree right)
        : InterpolatedExpressionTree
    {
        public string Operator { get; } = @operator.Trim();
        public InterpolatedExpressionTree Left { get; } = left;
        public InterpolatedExpressionTree Right { get; } = right;

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

        public override int GetHashCode() =>
            Operator.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode();

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is BinaryNode that
            && this.Left.Equals(that.Left)
            && this.Right.Equals(that.Right);
    }

    private class IndexerNode(InterpolatedExpressionTree body, InterpolatedExpressionTree index)
        : InterpolatedExpressionTree
    {
        public InterpolatedExpressionTree Body { get; } = body;
        public InterpolatedExpressionTree Index { get; } = index;

        public override bool IsSupported =>
            Body.IsSupported && Index.IsSupported;

        public override void Render(RenderingContext context) {
            context.Append(Body);
            context.Append("[");
            context.Append(Index);
            context.Append("]");
        }

        public override int GetHashCode() =>
            Body.GetHashCode() ^ Index.GetHashCode();

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is IndexerNode that
            && this.Body.Equals(that.Body)
            && this.Index.Equals(that.Index);
    }

    private class InstanceCallNode(InterpolatedExpressionTree body, string method, IReadOnlyList<InterpolatedExpressionTree> args)
        : InterpolatedExpressionTree
    {
        public InterpolatedExpressionTree Body { get; } = body;
        public string Method { get; } = method;
        public IReadOnlyList<InterpolatedExpressionTree> Args { get; } = args;

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

        public override int GetHashCode() =>
            Args.Aggregate(Body.GetHashCode() ^ Method.GetHashCode(), (h, a) => h ^ a.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is InstanceCallNode that
            && this.Body.Equals(that.Body)
            && this.Method.Equals(that.Method)
            && this.Args.SequenceEqual(that.Args);
    }

    private class LambdaNode(IReadOnlyList<InterpolatedExpressionTree> args, InterpolatedExpressionTree body)
        : InterpolatedExpressionTree
    {
        public IReadOnlyList<InterpolatedExpressionTree> Args { get; } = args;
        public InterpolatedExpressionTree Body { get; } = body;

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

        public override int GetHashCode() =>
            Args.Aggregate(Body.GetHashCode(), (h, a) => h ^ a.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is LambdaNode that
            && this.Body.Equals(that.Body)
            && this.Args.SequenceEqual(that.Args);
    }

    private class InitializerNode(InterpolatedExpressionTree body, IReadOnlyList<InterpolatedExpressionTree> initializers)
         : InterpolatedExpressionTree
    {
        public InterpolatedExpressionTree Body { get; } = body;
        public IReadOnlyList<InterpolatedExpressionTree> Initializers { get; } = initializers;

        public override bool IsSupported =>
            Body.IsSupported && Initializers.All(i => i.IsSupported);

        public override void Render(RenderingContext context) {
            context.Append(Body);
            context.Append(" {");
            if(Initializers.Count != 0) {
                context.Indent(Initializers[0]);
                for(var i = 1; i < Initializers.Count; i++) {
                    context.Append(",");
                    context.AppendNewLine();
                    context.Indent(Initializers[i]);
                }
            }
            context.AppendNewLine();
            context.Append("}");
        }

        public override int GetHashCode() =>
            Initializers.Aggregate(Body.GetHashCode(), (h, i) => h ^ i.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is InitializerNode that
            && this.Body.Equals(that.Body)
            && this.Initializers.SequenceEqual(that.Initializers);
    }

    private class ConcatNode(IReadOnlyList<InterpolatedExpressionTree> nodes) : InterpolatedExpressionTree {
        public IReadOnlyList<InterpolatedExpressionTree> Nodes { get; } = nodes;

        public ConcatNode(params InterpolatedExpressionTree[] nodes)
            : this((IReadOnlyList<InterpolatedExpressionTree>)nodes)
        { }

        public override bool IsSupported =>
            Nodes.All(n => n.IsSupported);

        public override void Render(RenderingContext context) {
            for(var i = 0; i < Nodes.Count; i++)
                context.Append(Nodes[i]);
        }

        public override int GetHashCode() =>
            Nodes.Aggregate(0, (h, n) => h ^ n.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is ConcatNode that
            && this.Nodes.SequenceEqual(that.Nodes);
    }

    private class MethodDefinitionNode(
        string method,
        IReadOnlyList<InterpolatedExpressionTree> parameters,
        IReadOnlyList<InterpolatedExpressionTree> typeConstraints,
        InterpolatedExpressionTree body
    ) : InterpolatedExpressionTree {
        public string Method { get; } = method;
        public IReadOnlyList<InterpolatedExpressionTree> Parameters { get; } = parameters;
        public IReadOnlyList<InterpolatedExpressionTree> TypeConstraints { get; } = typeConstraints;
        public InterpolatedExpressionTree Body { get; } = body;

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

        public override int GetHashCode() {
            var hash = Method.GetHashCode();
            hash = Parameters.Aggregate(hash, (h, p) => h ^ p.GetHashCode());
            hash = TypeConstraints.Aggregate(hash, (h, c) => h ^ c.GetHashCode());
            hash ^= Body.GetHashCode();
            return hash;
        }

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is MethodDefinitionNode that
            && this.Method.Equals(that.Method)
            && this.Parameters.SequenceEqual(that.Parameters)
            && this.TypeConstraints.SequenceEqual(that.TypeConstraints)
            && this.Body.Equals(that.Body);
    }

    private class ObjectInitNode(InterpolatedExpressionTree body, IReadOnlyList<InterpolatedExpressionTree> initializers)
        : InterpolatedExpressionTree
    {
        public InterpolatedExpressionTree Body { get; } = body;
        public IReadOnlyList<InterpolatedExpressionTree> Initializers { get; } = initializers;

        public override bool IsSupported =>
            Body.IsSupported && Initializers.All(static i => i.IsSupported);

        public override void Render(RenderingContext context) {
            context.Append(Body);
            context.Append(" {");
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

        public override int GetHashCode() =>
            Initializers.Aggregate(Body.GetHashCode(), static (h, i) => h ^ i.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is ObjectInitNode that
            && this.Body.Equals(that.Body)
            && this.Initializers.SequenceEqual(that.Initializers);
    }

    private class StaticCallNode(string method, IReadOnlyList<InterpolatedExpressionTree> args) : InterpolatedExpressionTree {
        public string Method { get; } = method;
        public IReadOnlyList<InterpolatedExpressionTree> Args { get; } = args;

        public override bool IsSupported =>
            Args.All(a => a.IsSupported);

        public override void Render(RenderingContext context) {
            context.AppendIndent(Method);
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

        public override int GetHashCode() =>
            Args.Aggregate(Method.GetHashCode(), (h, a) => h ^ a.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is StaticCallNode that
            && this.Method.Equals(that.Method)
            && this.Args.SequenceEqual(that.Args);
    }

    private class SwitchNode(
        InterpolatedExpressionTree subject,
        IReadOnlyList<InterpolatedExpressionTree> cases
    ) : InterpolatedExpressionTree {
        public InterpolatedExpressionTree Subject { get; } = subject;
        public IReadOnlyList<InterpolatedExpressionTree> Cases { get; } = cases;

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

        public override int GetHashCode() =>
            Cases.Aggregate(Subject.GetHashCode(), (h, n) => h ^ n.GetHashCode());

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is SwitchNode that
            && this.Subject.Equals(that.Subject)
            && this.Cases.SequenceEqual(that.Cases);
    }

    private class SwitchCaseNode(
        InterpolatedExpressionTree pattern,
        InterpolatedExpressionTree body
    ) : InterpolatedExpressionTree {
        public InterpolatedExpressionTree Pattern { get; } = pattern;
        public InterpolatedExpressionTree Body { get; } = body;

        public override bool IsSupported =>
            Pattern.IsSupported && Body.IsSupported;

        public override void Render(RenderingContext context) {
            context.Append(Pattern);
            context.Append(" => ");
            context.Append(Body);
        }

        public override int GetHashCode() =>
            Pattern.GetHashCode() ^ Body.GetHashCode();

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is SwitchCaseNode that
            && this.Pattern.Equals(that.Pattern)
            && this.Body.Equals(that.Body);
    }

    private class TernaryNode(InterpolatedExpressionTree condition, InterpolatedExpressionTree thenNode, InterpolatedExpressionTree elseNode)
        : InterpolatedExpressionTree
    {
        public InterpolatedExpressionTree Condition { get; } = condition;
        public InterpolatedExpressionTree ThenNode { get; } = thenNode;
        public InterpolatedExpressionTree ElseNode { get; } = elseNode;

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

        public override int GetHashCode() =>
            Condition.GetHashCode() ^ ThenNode.GetHashCode() ^ ElseNode.GetHashCode();

        public override bool Equals(InterpolatedExpressionTree? obj) =>
            obj is TernaryNode that
            && this.Condition.Equals(that.Condition)
            && this.ThenNode.Equals(that.ThenNode)
            && this.ElseNode.Equals(that.ElseNode);
    }
}
