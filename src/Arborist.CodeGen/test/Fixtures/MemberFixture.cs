namespace Arborist.CodeGen.Fixtures;

public class MemberFixture {
    public string InstanceField = default!;
    public static string StaticField = default!;
    public string InstanceProperty { get; } = default!;
    public static string StaticProperty { get; } = default!;
    public string this[string arg] => arg;
    public int InstanceMethod(string arg) => arg.Length;
    public static int StaticMethod(string arg) => arg.Length;
    public A GenericInstanceMethod<A>(A arg) => arg;
    public static A GenericStaticMethod<A>(A arg) => arg;
}
