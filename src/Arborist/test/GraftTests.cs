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

    [Fact]
    public void GraftNullable0_works_with_reference_intermediary_and_reference_result() {
        var expected = ExpressionOnNone.Of(() => default(Cat)!.Owner != null ? default(Cat)!.Owner.Name : null);

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Owner),
            ExpressionOn<Owner>.Of(o => o.Name)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_reference_intermediary_and_struct_result() {
        var expected = ExpressionOnNone.Of(
            () => default(Cat)!.Owner != null ? (Nullable<int>)default(Cat)!.Owner.Id : null
        );

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Owner),
            ExpressionOn<Owner>.Of(o => o.Id)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_reference_intermediary_and_nullable_result() {
        var expected = ExpressionOnNone.Of(
            () => default(Cat)!.Owner != null ? default(Cat)!.Owner.Age : null
        );

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Owner),
            ExpressionOn<Owner>.Of(o => o.Age)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_nullable_intermediary_and_reference_result() {
        var expected = ExpressionOnNone.Of(() => default(Cat)!.Age != null ? default(Cat)!.Age!.Value.ToString() : null);

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Age),
            ExpressionOn<int>.Of(i => i.ToString())
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_nullable_intermediary_and_struct_result() {
        var expected = ExpressionOnNone.Of(
            () => default(Cat)!.Age != null ? (Nullable<int>)default(Cat)!.Age!.Value : null
        );

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Age!),
            ExpressionOn<int>.Of(i => i)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_nullable_intermediary_and_nullable_result() {
        var expected = ExpressionOnNone.Of(
            () => default(Cat)!.Age != null ? new Nullable<int>(default(Cat)!.Age!.Value) : null
        );

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Age),
            ExpressionOn<int>.Of(i => new Nullable<int>(i))
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_reference_intermediary_and_reference_result() {
        var expected = ExpressionOn<Cat>.Of(c => c.Owner != null ? c.Owner.Name : null);

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Owner),
            ExpressionOn<Owner>.Of(o => o.Name)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_reference_intermediary_and_struct_result() {
        var expected = ExpressionOn<Cat>.Of(
            c => c.Owner != null ? (Nullable<int>)c.Owner.Id : null
        );

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Owner),
            ExpressionOn<Owner>.Of(o => o.Id)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_reference_intermediary_and_nullable_result() {
        var expected = ExpressionOn<Cat>.Of(
            c => c.Owner != null ? c.Owner.Age : null
        );

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Owner),
            ExpressionOn<Owner>.Of(o => o.Age)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_nullable_intermediary_and_reference_result() {
        var expected = ExpressionOn<Cat>.Of(c => c.Age != null ? c.Age.Value.ToString() : null);

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Age),
            ExpressionOn<int>.Of(i => i.ToString())
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_nullable_intermediary_and_struct_result() {
        var expected = ExpressionOn<Cat>.Of(
            c => c.Age != null ? (Nullable<int>)c.Age.Value : null
        );

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Age),
            ExpressionOn<int>.Of(i => i)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_nullable_intermediary_and_nullable_result() {
        var expected = ExpressionOn<Cat>.Of(
            c => c.Age != null ? new Nullable<int>(c.Age.Value) : null
        );

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Age),
            ExpressionOn<int>.Of(i => new Nullable<int>(i))
        );

        Assert.Equivalent(expected, actual);
    }
}
