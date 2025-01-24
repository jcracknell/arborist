namespace Arborist.TestFixtures;

public class MemberFixture {
    public MemberFixture() { }
    public MemberFixture(string str) { }
    public string InstanceField = default!;
    public static string StaticField = default!;
    public string InstanceProperty { get; set; } = default!;
    public static string StaticProperty { get; } = default!;
    public string this[string arg] => arg;
    public bool Method() => false;
    public bool Method(int i) => true;
    public int InstanceMethod(string arg) => arg.Length;
    public static int StaticMethod(string arg) => arg.Length;
    public A GenericInstanceMethod<A>(A arg) => arg;
    public static A GenericStaticMethod<A>(A arg) => arg;
}
