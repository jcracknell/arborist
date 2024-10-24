using Arborist.Fixtures;
using System.Reflection;

namespace Arborist;

public partial class ExpressionThunkTests {
    [Fact]
    public void GetConstructor_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetConstructor(Array.Empty<Type>());
        var actual = ExpressionThunk.GetConstructor(() => new MemberFixture());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetMethod_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetMethod(nameof(MemberFixture.Method), Array.Empty<Type>());
        var actual = ExpressionThunk.GetMethod(() => default(MemberFixture)!.Method());

        Assert.Equal(expected, actual);
    }
}
