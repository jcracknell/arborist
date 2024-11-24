namespace Arborist.CodeGen.Fixtures;

public class Owner {
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public IEnumerable<Cat> Cats { get; set; } = default!;
}

public class Cat {
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int? Age { get; set; }
    public Owner Owner { get; set; } = default!;
    public Cat Mother { get; set; } = default!;
    public Cat? Father { get; set; }
    public bool IsAlive { get; set; }
}
