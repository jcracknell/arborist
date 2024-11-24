namespace Arborist.CodeGen;

public class LocalDefinition {
    private readonly Func<int> _orderProvider;
    private InterpolatedExpressionTree? _declaration;

    public LocalDefinition(string identifier, Func<int> orderProvider) {
        _orderProvider = orderProvider;
        Identifier = identifier;
        Order = int.MaxValue;
        _declaration = default;
    }

    /// <summary>
    /// The identifier (variable name) used to reference this definition.
    /// </summary>
    public string Identifier { get; }

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// The order in which the definition should be defined
    /// (definitions may reference other earlier definitions).
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// The expression used to initialize the value of this definition.
    /// </summary>
    public InterpolatedExpressionTree Initializer =>
        _declaration ?? InterpolatedExpressionTree.Unsupported;

    public void SetInitializer(InterpolatedExpressionTree value) {
        _declaration = value;
        Order = _orderProvider();
        IsInitialized = true;
    }

    public override string ToString() =>
        $"var {Identifier} = {Initializer};";

    public class Factory {
        private readonly Func<int> _orderProvider;

        public Factory(Func<int> orderProvider) {
            _orderProvider = orderProvider;
        }

        public LocalDefinition Create(string name) =>
            new LocalDefinition(name, _orderProvider);
    }
}
