namespace Arborist.TestFixtures;

public class NestedCollectionInitializerFixture<A> {
    public List<A> List { get; set; } = default!;
    public Dictionary<string, A> Dictionary { get; set; } = default!;
}
