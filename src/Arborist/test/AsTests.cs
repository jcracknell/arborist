using Arborist.TestFixtures;
using Arborist.Utils;

namespace Arborist;

public class AsTests {
    [Fact]
    public void Should_work_as_expected_for_Func1() {
        var expected = ExpressionOnNone.Interpolate(x => x.SpliceConstant(default(object)) as Owner);
        var actual = ExpressionHelper.As(ExpressionOnNone.Of(() => default(object)), TypeOf<Owner>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Owner), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func2() {
        var expected = ExpressionOn<Cat>.Of(c => (object)c.Owner as Owner);
        var actual = ExpressionHelper.As(ExpressionOn<Cat>.Of(c => (object)c.Owner), TypeOf<Owner>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Owner), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func3() {
        var expected = ExpressionOn<Cat, Cat>.Of((c, d) => (object)c.Owner as Owner);
        var actual = ExpressionHelper.As(ExpressionOn<Cat, Cat>.Of((c, d) => (object)c.Owner), TypeOf<Owner>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Owner), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func4() {
        var expected = ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => (object)c.Owner as Owner);
        var actual = ExpressionHelper.As(ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => (object)c.Owner), TypeOf<Owner>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Owner), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func5() {
        var expected = ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => (object)c.Owner as Owner);
        var actual = ExpressionHelper.As(ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => (object)c.Owner), TypeOf<Owner>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Owner), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func1_nullable() {
        var expected = ExpressionOnNone.Interpolate(x => x.SpliceConstant(default(object)) as Nullable<int>);
        var actual = ExpressionHelper.As(ExpressionOnNone.Of(() => default(object)), TypeOf<Nullable<int>>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Nullable<int>), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func2_nullable() {
        var expected = ExpressionOn<Cat>.Of(c => (object)c.Owner as Nullable<int>);
        var actual = ExpressionHelper.As(ExpressionOn<Cat>.Of(c => (object)c.Owner), TypeOf<Nullable<int>>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Nullable<int>), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func3_nullable() {
        var expected = ExpressionOn<Cat, Cat>.Of((c, d) => (object)c.Owner as Nullable<int>);
        var actual = ExpressionHelper.As(ExpressionOn<Cat, Cat>.Of((c, d) => (object)c.Owner), TypeOf<Nullable<int>>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Nullable<int>), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func4_nullable() {
        var expected = ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => (object)c.Owner as Nullable<int>);
        var actual = ExpressionHelper.As(ExpressionOn<Cat, Cat, Cat>.Of((c, d, e) => (object)c.Owner), TypeOf<Nullable<int>>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Nullable<int>), actual.ReturnType);
    }

    [Fact]
    public void Should_work_as_expected_for_Func5_nullable() {
        var expected = ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => (object)c.Owner as Nullable<int>);
        var actual = ExpressionHelper.As(ExpressionOn<Cat, Cat, Cat, Cat>.Of((c, d, e, f) => (object)c.Owner), TypeOf<Nullable<int>>.Value);

        Assert.Equivalent(expected, actual);
        Assert.Equal(ExpressionType.TypeAs, actual.Body.NodeType);
        Assert.Equal(typeof(Nullable<int>), actual.ReturnType);
    }
}
