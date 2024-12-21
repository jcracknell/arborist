namespace Arborist.TestFixtures;

public class Cat {
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public int? Age { get; init; }
    public decimal? Weight { get; init; }
    public Owner Owner { get; init; } = default!;
    public bool IsAlive { get; init; }
}
