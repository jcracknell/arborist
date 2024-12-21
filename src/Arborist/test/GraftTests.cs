using Arborist.TestFixtures;

namespace Arborist;

public class GraftTests {
    [Fact]
    public void Graft0_works_as_expected() {
        var expected = ExpressionOnNone.Of(() => "foo".Length);
        var actual = ExpressionOnNone.Graft(() => "foo", v => v.Length);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Graft1_works_as_expected() {
        var expected = ExpressionOn<Cat>.Of(a => a.Name.Length);
        var actual = ExpressionOn<Cat>.Graft(a => a.Name, v => v.Length);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Graft2_works_as_expected() {
        var expected = ExpressionOn<Cat, Cat>.Of((a, b) => a.Name.Length);
        var actual = ExpressionOn<Cat, Cat>.Graft((a, b) => a.Name, v => v.Length);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Graft3_works_as_expected() {
        var expected = ExpressionOn<Cat, Cat, Cat>.Of((a, b, c) => a.Name.Length);
        var actual = ExpressionOn<Cat, Cat, Cat>.Graft((a, b, c) => a.Name, v => v.Length);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Graft4_works_as_expected() {
        var expected = ExpressionOn<Cat, Cat, Cat, Cat>.Of((a, b, c, d) => a.Name.Length);
        var actual = ExpressionOn<Cat, Cat, Cat, Cat>.Graft((a, b, c, d) => a.Name, v => v.Length);

        Assert.Equivalent(expected, actual);
    }
}
