namespace Arborist.TestFixtures;

public class Owner {
    public int Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public IEnumerable<Cat> Cats { get; set; } = default!;
    public IEnumerable<Dog> Dogs { get; set; } = default!;
    public IEnumerable<Cat> CatsEnumerable { get; set; } = default!;
    public IQueryable<Cat> CatsQueryable { get; set; } = default!;
}
