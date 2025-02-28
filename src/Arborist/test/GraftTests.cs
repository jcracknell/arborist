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
        var expected = ExpressionOnNone.Of(() => default(Cat)!.Owner == null ? null : default(Cat)!.Owner.Name);

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Owner),
            ExpressionOn<Owner>.Of(o => o.Name)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_reference_intermediary_and_struct_result() {
        var expected = ExpressionOnNone.Of(
            () => default(Cat)!.Owner == null ? null : (Nullable<int>)default(Cat)!.Owner.Id
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
            () => default(Cat)!.Owner == null ? null : default(Cat)!.Owner.Age
        );

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Owner),
            ExpressionOn<Owner>.Of(o => o.Age)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_nullable_intermediary_and_reference_result() {
        var expected = ExpressionOnNone.Of(() => default(Cat)!.Age == null ? null : default(Cat)!.Age!.Value.ToString());

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Age),
            ExpressionOn<int>.Of(i => i.ToString())
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable0_works_with_nullable_intermediary_and_struct_result() {
        var expected = ExpressionOnNone.Of(
            () => default(Cat)!.Age == null ? null : (Nullable<int>)default(Cat)!.Age!.Value
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
            () => default(Cat)!.Age == null ? null : new Nullable<int>(default(Cat)!.Age!.Value)
        );

        var actual = ExpressionOnNone.GraftNullable(
            ExpressionOnNone.Of(() => default(Cat)!.Age),
            ExpressionOn<int>.Of(i => new Nullable<int>(i))
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_reference_intermediary_and_reference_result() {
        var expected = ExpressionOn<Cat>.Of(c => c.Owner == null ? null : c.Owner.Name);

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Owner),
            ExpressionOn<Owner>.Of(o => o.Name)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_reference_intermediary_and_struct_result() {
        var expected = ExpressionOn<Cat>.Of(
            c => c.Owner == null ? null : (Nullable<int>)c.Owner.Id
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
            c => c.Owner == null ? null : c.Owner.Age
        );

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Owner),
            ExpressionOn<Owner>.Of(o => o.Age)
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_nullable_intermediary_and_reference_result() {
        var expected = ExpressionOn<Cat>.Of(c => c.Age == null ? null : c.Age.Value.ToString());

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Age),
            ExpressionOn<int>.Of(i => i.ToString())
        );

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void GraftNullable1_works_with_nullable_intermediary_and_struct_result() {
        var expected = ExpressionOn<Cat>.Of(
            c => c.Age == null ? null : (Nullable<int>)c.Age.Value
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
            c => c.Age == null ? null : new Nullable<int>(c.Age.Value)
        );

        var actual = ExpressionOn<Cat>.GraftNullable(
            ExpressionOn<Cat>.Of(c => c.Age),
            ExpressionOn<int>.Of(i => new Nullable<int>(i))
        );

        Assert.Equivalent(expected, actual);
    }
}
