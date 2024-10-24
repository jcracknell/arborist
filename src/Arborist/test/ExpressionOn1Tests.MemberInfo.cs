using Arborist.Fixtures;
using BindingFlags = System.Reflection.BindingFlags;

namespace Arborist;

public partial class ExpressionOn1Tests {
    [Fact]
    public void GetConstructor_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetConstructor(Array.Empty<Type>());
        var actual = ExpressionOn<string>.GetConstructor(s => new MemberFixture());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetMethod_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetMethod(nameof(MemberFixture.Method), Array.Empty<Type>());
        var actual = ExpressionOn<MemberFixture>.GetMethod(m => m.Method());

        Assert.Equal(expected, actual);
    }
}
