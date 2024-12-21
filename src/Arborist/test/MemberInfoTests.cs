using Arborist.TestFixtures;

namespace Arborist;

public class MemberInfoTests {
    [Fact]
    public void GetConstructorInfo0_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetConstructor(Array.Empty<Type>());
        var actual = ExpressionOnNone.GetConstructorInfo(() => new MemberFixture());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetMethodInfo0_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetMethod(nameof(MemberFixture.Method), Array.Empty<Type>());
        var actual = ExpressionOnNone.GetMethodInfo(() => default(MemberFixture)!.Method());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetConstructorInfo1_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetConstructor(Array.Empty<Type>());
        var actual = ExpressionOn<string>.GetConstructorInfo(s => new MemberFixture());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetMethodInfo1_should_work_as_expected() {
        var expected = typeof(MemberFixture).GetMethod(nameof(MemberFixture.Method), Array.Empty<Type>());
        var actual = ExpressionOn<MemberFixture>.GetMethodInfo(m => m.Method());

        Assert.Equal(expected, actual);
    }
}
