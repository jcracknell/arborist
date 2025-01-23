namespace Arborist.CodeGen;

public sealed class InterpolatedValueDefinition : IEquatable<InterpolatedValueDefinition> {
    private InterpolatedTree? _initializer;

    public InterpolatedValueDefinition(string identifier) {
        Identifier = identifier;
        Order = int.MaxValue;
        _initializer = default;
    }

    /// <summary>
    /// The identifier (variable name) used to reference this definition.
    /// </summary>
    public string Identifier { get; }

    public bool IsInitialized =>
        _initializer is not null;

    /// <summary>
    /// The order in which the definition should be defined
    /// (definitions may reference other earlier definitions).
    /// </summary>
    public int Order { get; private set; }

    public bool IsSupported =>
        Initializer.IsSupported;

    /// <summary>
    /// The expression used to initialize the value of this definition.
    /// </summary>
    public InterpolatedTree Initializer =>
        _initializer ?? throw new InvalidOperationException(nameof(Initializer));

    private void SetInitializer(int order, InterpolatedTree value) {
        Order = order;
        _initializer = value;
    }

    public override int GetHashCode() =>
        Identifier.GetHashCode();

    public override bool Equals(object? obj) =>
        Equals(obj as InterpolatedValueDefinition);

    public bool Equals(InterpolatedValueDefinition? that) =>
        that is not null
        && this.Identifier.Equals(that.Identifier)
        && this.IsInitialized == that.IsInitialized
        && (!this.IsInitialized || this.Initializer.Equals(that.Initializer));

    public override string ToString() =>
        $"var {Identifier} = {Initializer};";

    public sealed class Factory {
        private readonly Func<int> _orderProvider;

        public Factory(Func<int> orderProvider) {
            _orderProvider = orderProvider;
        }

        public InterpolatedValueDefinition Create(string name) =>
            new InterpolatedValueDefinition(name);

        /// <summary>
        /// Sets the <paramref name="initializer"/> value for the provided <paramref name="definition"/>,
        /// and establishes the order in which the definition must be declared in relation to the other
        /// definitions.
        /// </summary>
        public void Set(InterpolatedValueDefinition definition, InterpolatedTree initializer) {
            definition.SetInitializer(_orderProvider(), initializer);
        }
    }
}
