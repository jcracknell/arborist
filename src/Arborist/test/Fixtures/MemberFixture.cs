namespace Arborist.Fixtures;

public class MemberFixture {
    public MemberFixture() { }
    public MemberFixture(string str) { }
    public string Field = default!;
    public string Property { get; set; } = default!;
    public bool Method() => false;
    public bool Method(int i) => true;
}
