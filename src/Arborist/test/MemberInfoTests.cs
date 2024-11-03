using Arborist.Fixtures;

namespace Arborist;

public class MemberInfoTests {
    [Fact]
    public void GetConstructor0_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetConstructor(Array.Empty<Type>());
        var actual = ExpressionOnNone.GetConstructor(() => new MemberFixture());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetMethod0_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetMethod(nameof(MemberFixture.Method), Array.Empty<Type>());
        var actual = ExpressionOnNone.GetMethod(() => default(MemberFixture)!.Method());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetConstructor1_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetConstructor(Array.Empty<Type>());
        var actual = ExpressionOn<string>.GetConstructor(s => new MemberFixture());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetMethod1_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetMethod(nameof(MemberFixture.Method), Array.Empty<Type>());
        var actual = ExpressionOn<MemberFixture>.GetMethod(m => m.Method());

        Assert.Equal(expected, actual);
    }
}
