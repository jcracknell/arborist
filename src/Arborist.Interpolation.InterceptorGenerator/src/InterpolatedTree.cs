namespace Arborist.Interpolation.InterceptorGenerator;

public abstract class InterpolatedTree : IEquatable<InterpolatedTree> {
    public static InterpolatedTree Unsupported { get; } = new UnsupportedNode();

    /// <summary>
    /// Singleton empty <see cref="InterpolatedTree"/> value.
    /// </summary>
    public static InterpolatedTree Empty { get; } = Verbatim("");

    public static InterpolatedTree Verbatim(string value) =>
        VerbatimNode.Create(value);

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

    /// <summary>
    /// Binds the provided <paramref name="value"/> to a variable with the specified <paramref name="identifier"/>
    /// using a single-arm switch expression with the provided <paramref name="body"/>.
    /// </summary>
    public static InterpolatedTree Bind(string identifier, InterpolatedTree value, InterpolatedTree body) =>
        Switch(value, [SwitchCase(Interpolate($"var {identifier}"), body)]);

    public static InterpolatedTree Call(
        InterpolatedTree method,
        IReadOnlyList<InterpolatedTree> args
    ) =>
        new CallNode(method, args);

    public static InterpolatedTree Concat(params InterpolatedTree[] nodes) =>
        new ConcatNode(nodes);

    public static InterpolatedTree Concat(IReadOnlyList<InterpolatedTree> nodes) =>
        new ConcatNode(nodes);

    public static InterpolatedTree Initializer(IReadOnlyList<InterpolatedTree> elements) =>
        new InitializerNode(elements);

    /// <summary>
    /// Creates an <see cref="InterpolatedTree"/> from the provided interpolated string, interpolating
    /// non-<see cref="InterpolatedTree"/> values as <see cref="Verbatim"/> trees.
    /// </summary>
    /// <seealso cref="Concat(IReadOnlyList{InterpolatedTree})"/>
    public static InterpolatedTree Interpolate(ref InterpolationHandler interpolated) =>
        interpolated.GetTree();

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
        InterpolatedTree method,
        IReadOnlyList<InterpolatedTree> parameters,
        IReadOnlyList<InterpolatedTree> typeConstraints,
        InterpolatedTree body
    ) =>
        new MethodDefinitionNode(method, parameters, typeConstraints, body);

    public static InterpolatedTree Switch(
        InterpolatedTree subject,
        IReadOnlyList<InterpolatedTree> cases
    ) =>
        new SwitchNode(subject, cases);

    public static InterpolatedTree SwitchCase(
        InterpolatedTree pattern,
        InterpolatedTree body
    ) =>
        Concat(pattern, Verbatim(" => "), body);

    public static InterpolatedTree Ternary(
        InterpolatedTree condition,
        InterpolatedTree thenNode,
        InterpolatedTree elseNode
    ) =>
        new TernaryNode(condition, thenNode, elseNode);

    private bool _isModified;
    public abstract bool IsSupported { get; }
    public abstract InterpolatedTree AsModified();
    protected abstract bool ChildrenModified();
    public abstract void Render(ref RenderingContext context);

    public bool IsModified {
        get { return _isModified || ChildrenModified(); }
        protected set { _isModified = value; }
    }

    public void WriteTo(PooledStringWriter writer, int level) {
        var context = new RenderingContext(writer, level);
        Render(ref context);
    }

    public abstract override int GetHashCode();
    public abstract bool Equals(InterpolatedTree? obj);

    public override bool Equals(object? obj) =>
        Equals(obj as InterpolatedTree);

    public override string ToString() =>
        ToString(0);

    public string ToString(int level) {
        using var writer = PooledStringWriter.Rent();
        WriteTo(writer, level);
        return writer.ToString();
    }

    public ref struct RenderingContext(PooledStringWriter writer, int level) {
        private const string INDENT = "    ";

        public void Indent() {
            level += 1;
        }

        public void Dedent() {
            level -= 1;
        }

        public void Append(string value) {
            writer.Write(value);
        }

        public void Append(InterpolatedTree node) {
            node.Render(ref this);
        }

        public void AppendIndent() {
            for(var ci = writer.Length - 1; 0 < ci; ci--) {
                var c = writer[ci];
                if(c == '\n')
                    break;
                // Skip indentation if the buffered output does not end with a newline followed by whitespace
                if(!Char.IsWhiteSpace(c))
                    return;
            }

            for(var i = 0; i < level; i++)
                writer.Write(INDENT);
        }

        public void AppendIndent(string value) {
            AppendIndent();
            Append(value);
        }

        public void Indent(InterpolatedTree node) {
            level += 1;
            node.Render(ref this);
            level -= 1;
        }

        public void AppendNewLine() {
            writer.WriteLine("");
        }
    }

    private class UnsupportedNode : InterpolatedTree {
        public UnsupportedNode() {
            IsModified = true;
        }

        public override bool IsSupported => false;

        public override InterpolatedTree AsModified() => this;

        protected override bool ChildrenModified() => false;

        public override void Render(ref RenderingContext context) {
            context.AppendIndent("???");
        }

        public override int GetHashCode() =>
            "???".GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is UnsupportedNode;
    }

    private sealed class VerbatimNode : InterpolatedTree {
        private static ImmutableDictionary<string, VerbatimNode> _instances =
            ImmutableDictionary<string, VerbatimNode>.Empty;

        public static VerbatimNode Create(string value) {
            if(_instances.TryGetValue(value, out var instance))
                return instance;

            instance = new VerbatimNode(value);
            if(IsCacheable(value))
                _instances = _instances.SetItem(value, instance);

            return instance;
        }

        private static bool IsCacheable(string value) {
            if(8 < value.Length)
                return false;

            for(var i = 0; i < value.Length; i++) {
                var c = value[i];
                if(!Char.IsWhiteSpace(c) && !Char.IsPunctuation(c))
                    return false;
            }

            return true;
        }

        private VerbatimNode(string value) {
            Value = value;
        }

        public string Value { get; }

        public override bool IsSupported => true;

        public override InterpolatedTree AsModified() =>
            new VerbatimNode(Value) { IsModified = true };

        protected override bool ChildrenModified() => false;

        public override void Render(ref RenderingContext context) {
            context.AppendIndent(Value);
        }

        public override int GetHashCode() =>
            Value.GetHashCode();

        public override bool Equals(InterpolatedTree? obj) =>
            obj is VerbatimNode that && this.Value.Equals(that.Value);
    }

    private class ArrowBodyNode(InterpolatedTree expression) : InterpolatedTree {
        public InterpolatedTree Expression { get; } = expression;

        public override bool IsSupported =>
            Expression.IsSupported;

        public override InterpolatedTree AsModified() =>
            new ArrowBodyNode(Expression) { IsModified = true };

        protected override bool ChildrenModified() =>
            Expression.IsModified;

        public override void Render(ref RenderingContext context) {
            context.Indent();
            context.AppendIndent("=> ");
            context.Append(Expression);
            context.Append(";");
            context.Dedent();
        }

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

        public override InterpolatedTree AsModified() =>
            new BinaryNode(Operator, Left, Right) { IsModified = true };

        protected override bool ChildrenModified() =>
            Left.IsModified || Right.IsModified;

        public override void Render(ref RenderingContext context) {
            context.Append("(");
            context.Append(Left);
            context.Append(" ");
            context.Append(Operator);
            context.Append(" ");
            context.Append(Right);
            context.Append(")");
        }

        public override int GetHashCode() =>
            HashCode.Combine(Operator, Left, Right);

        public override bool Equals(InterpolatedTree? obj) =>
            obj is BinaryNode that
            && this.Left.Equals(that.Left)
            && this.Right.Equals(that.Right);
    }

    private class LambdaNode(IReadOnlyList<InterpolatedTree> args, InterpolatedTree body)
        : InterpolatedTree
    {
        public IReadOnlyList<InterpolatedTree> Args { get; } = args;
        public InterpolatedTree Body { get; } = body;

        public override bool IsSupported =>
            Body.IsSupported && Args.All(a => a.IsSupported);

        public override InterpolatedTree AsModified() =>
            new LambdaNode(Args, Body) { IsModified = true };

        protected override bool ChildrenModified() =>
            Body.IsModified || Args.Any(a => a.IsModified);

        public override void Render(ref RenderingContext context) {
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

        public override int GetHashCode() {
            var hash = new HashCode();
            hash = hash.AddRange(Args);
            return hash.ToHashCode();
        }

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

        public override InterpolatedTree AsModified() =>
            new InitializerNode(Initializers) { IsModified = true };

        protected override bool ChildrenModified() =>
            Initializers.Any(i => i.IsModified);

        public override void Render(ref RenderingContext context) {
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

        public override int GetHashCode() {
            var hash = new HashCode();
            hash = hash.AddRange(Initializers);
            return hash.ToHashCode();
        }

        public override bool Equals(InterpolatedTree? obj) =>
            obj is InitializerNode that
            && this.Initializers.SequenceEqual(that.Initializers);
    }

    private class CallNode(InterpolatedTree expression, IReadOnlyList<InterpolatedTree> args) : InterpolatedTree {
        public InterpolatedTree Expression { get; } = expression;
        public IReadOnlyList<InterpolatedTree> Args { get; } = args;

        public override bool IsSupported =>
            Expression.IsSupported
            && Args.All(a => a.IsSupported);

        public override InterpolatedTree AsModified() =>
            new CallNode(Expression, Args) { IsModified = true };

        protected override bool ChildrenModified() =>
            Expression.IsModified || Args.Any(a => a.IsModified);

        public override void Render(ref RenderingContext context) {
            context.Append(Expression);
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

        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(Expression);
            hash = hash.AddRange(Args);
            return hash.ToHashCode();
        }

        public override bool Equals(InterpolatedTree? obj) =>
            obj is CallNode that
            && this.Expression.Equals(that.Expression)
            && this.Args.SequenceEqual(that.Args);
    }

    private class ConcatNode(IReadOnlyList<InterpolatedTree> nodes) : InterpolatedTree {
        public IReadOnlyList<InterpolatedTree> Nodes { get; } = nodes;

        public ConcatNode(params InterpolatedTree[] nodes)
            : this((IReadOnlyList<InterpolatedTree>)nodes)
        { }

        public override bool IsSupported =>
            Nodes.All(n => n.IsSupported);

        public override InterpolatedTree AsModified() =>
            new ConcatNode(Nodes) { IsModified = true };

        protected override bool ChildrenModified() =>
            Nodes.Any(n => n.IsModified);

        public override void Render(ref RenderingContext context) {
            for(var i = 0; i < Nodes.Count; i++)
                context.Append(Nodes[i]);
        }

        public override int GetHashCode() {
            var hash = new HashCode();
            hash = hash.AddRange(Nodes);
            return hash.ToHashCode();
        }

        public override bool Equals(InterpolatedTree? obj) =>
            obj is ConcatNode that
            && this.Nodes.SequenceEqual(that.Nodes);
    }

    private class MethodDefinitionNode(
        InterpolatedTree method,
        IReadOnlyList<InterpolatedTree> parameters,
        IReadOnlyList<InterpolatedTree> typeConstraints,
        InterpolatedTree body
    ) : InterpolatedTree {
        public InterpolatedTree Method { get; } = method;
        public IReadOnlyList<InterpolatedTree> Parameters { get; } = parameters;
        public IReadOnlyList<InterpolatedTree> TypeConstraints { get; } = typeConstraints;
        public InterpolatedTree Body { get; } = body;

        public override bool IsSupported =>
            Method.IsSupported
            && Body.IsSupported
            && Parameters.All(p => p.IsSupported)
            && TypeConstraints.All(c => c.IsSupported);

        public override InterpolatedTree AsModified() =>
            new MethodDefinitionNode(Method, Parameters, TypeConstraints, Body) { IsModified = true };

        protected override bool ChildrenModified() =>
            Method.IsModified
            || Body.IsModified
            || Parameters.Any(p => p.IsModified)
            || TypeConstraints.Any(c => c.IsModified);

        public override void Render(ref RenderingContext context) {
            context.Append(Method);
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
                context.Indent();
                for(var ci = 0; ci < TypeConstraints.Count; ci++) {
                    context.AppendNewLine();
                    context.AppendIndent("where ");
                    context.Append(TypeConstraints[ci]);
                }
                context.Dedent();
            }
            context.AppendNewLine();
            // Avoid indenting empty method body
            if(!Empty.Equals(Body))
                context.Append(Body);
        }

        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(Method);
            hash = hash.AddRange(Parameters);
            hash = hash.AddRange(TypeConstraints);
            hash.Add(Body);
            return hash.ToHashCode();
        }

        public override bool Equals(InterpolatedTree? obj) =>
            obj is MethodDefinitionNode that
            && this.Method.Equals(that.Method)
            && this.Parameters.SequenceEqual(that.Parameters)
            && this.TypeConstraints.SequenceEqual(that.TypeConstraints)
            && this.Body.Equals(that.Body);
    }

    private class SwitchNode(
        InterpolatedTree subject,
        IReadOnlyList<InterpolatedTree> cases
    ) : InterpolatedTree {
        public InterpolatedTree Subject { get; } = subject;
        public IReadOnlyList<InterpolatedTree> Cases { get; } = cases;

        public override bool IsSupported =>
            Subject.IsSupported && Cases.All(n => n.IsSupported);

        public override InterpolatedTree AsModified() =>
            new SwitchNode(Subject, Cases) { IsModified = true };

        protected override bool ChildrenModified() =>
            Subject.IsModified || Cases.Any(c => c.IsModified);

        public override void Render(ref RenderingContext context) {
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

        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(Subject);
            hash = hash.AddRange(Cases);
            return hash.ToHashCode();
        }

        public override bool Equals(InterpolatedTree? obj) =>
            obj is SwitchNode that
            && this.Subject.Equals(that.Subject)
            && this.Cases.SequenceEqual(that.Cases);
    }

    private class TernaryNode(InterpolatedTree condition, InterpolatedTree thenNode, InterpolatedTree elseNode)
        : InterpolatedTree
    {
        public InterpolatedTree Condition { get; } = condition;
        public InterpolatedTree ThenNode { get; } = thenNode;
        public InterpolatedTree ElseNode { get; } = elseNode;

        public override bool IsSupported =>
            Condition.IsSupported && ThenNode.IsSupported && ElseNode.IsSupported;

        public override InterpolatedTree AsModified() =>
            new TernaryNode(Condition, ThenNode, ElseNode) { IsModified = true };

        protected override bool ChildrenModified() =>
            Condition.IsModified || ThenNode.IsModified || ElseNode.IsModified;

        public override void Render(ref RenderingContext context) {
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
            HashCode.Combine(Condition, ThenNode, ElseNode);

        public override bool Equals(InterpolatedTree? obj) =>
            obj is TernaryNode that
            && this.Condition.Equals(that.Condition)
            && this.ThenNode.Equals(that.ThenNode)
            && this.ElseNode.Equals(that.ElseNode);
    }

    [global::System.Runtime.CompilerServices.InterpolatedStringHandler]
    public ref struct InterpolationHandler {
        private readonly List<InterpolatedTree> _trees;
        private readonly PooledStringWriter _writer;
        private bool _consumed;

        public InterpolationHandler(int literalCount, int interpolatedCount) {
            _trees = new List<InterpolatedTree>(literalCount + interpolatedCount);
            _writer = PooledStringWriter.Rent();
            _consumed = false;
        }

        private void AssertUnconsumed() {
            if(_consumed)
                throw new InvalidOperationException($"Attempt to access consumed {nameof(InterpolationHandler)}.");
        }

        public InterpolatedTree GetTree() {
            AssertUnconsumed();

            AppendBufferedLiteral();
            _writer.Dispose();
            _consumed = true;

            return _trees.Count switch {
                0 => Empty,
                1 => _trees[0],
                _ => Concat(_trees)
            };
        }

        private void AppendBufferedLiteral() {
            if(_writer.Length == 0)
                return;

            _trees.Add(Verbatim(_writer.ToString()));
            _writer.Clear();
        }

        public void AppendLiteral(string? literal) {
            AssertUnconsumed();

            if(literal is not null && literal.Length != 0)
                _writer.Write(literal);
        }

        public void AppendFormatted(object? obj) {
            AssertUnconsumed();

            if(obj is InterpolatedTree tree) {
                AppendBufferedLiteral();
                _trees.Add(tree);
            } else {
                AppendLiteral(obj?.ToString());
            }
        }
    }
}
