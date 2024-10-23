namespace Arborist.Fixtures;

public class Owner {
    public int Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public IEnumerable<Cat> CatsEnumerable { get; init; } = default!;
    public IQueryable<Cat> CatsQueryable { get; init; } = default!;
}
