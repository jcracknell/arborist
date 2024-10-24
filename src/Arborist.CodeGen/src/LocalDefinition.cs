namespace Arborist.CodeGen;

internal class LocalDefinition {
    private string? _declaration = default;

    public LocalDefinition(LocalDefinitionType type, string identifer, int order) {
        Type = type;
        Identifier = identifer;
        Order = order;
    }

    public LocalDefinitionType Type { get; }

    public string Identifier { get; }

    public int Order { get; }

    public string Declaration {
        get { return _declaration ?? throw new InvalidOperationException($"Uninitialized {nameof(LocalDefinition)}."); }
        set { _declaration = value; }
    }
}
