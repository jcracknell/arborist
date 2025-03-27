using Arborist.Interpolation;
using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_an_unquoted_lambda() {
        var interpolated = ExpressionOn<Owner>.Interpolate(
            default(object),
            (x, o) => o.CatsEnumerable.Any(c => c.Name == x.SpliceConstant("Garfield"))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsEnumerable.Any(c => c.Name == "Garfield"));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_a_quoted_lambda() {
        var interpolated = ExpressionOn<Owner>.Interpolate(
            default(object),
            (x, o) => o.CatsQueryable.Any(c => c.Name == x.SpliceConstant("Garfield"))
        );

        var expected = ExpressionOn<Owner>.Of(o => o.CatsQueryable.Any(c => c.Name == "Garfield"));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_instance_call() {
        var interpolated = ExpressionOnNone.Interpolate(
            x => x.SpliceConstant(42).Equals(x.SpliceConstant(42))
        );

        var expected = ExpressionOnNone.Of(() => 42.Equals(42));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_static_call() {
        var interpolated = ExpressionOnNone.Interpolate(
            x => ImmutableList.Create(x.SpliceConstant(42))
        );

        var expected = ExpressionOnNone.Of(() => ImmutableList.Create(42));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_postfix_extension_method_call() {
        var interpolated = ExpressionOnNone.Interpolate(
            x => x.SpliceConstant("foo").Any(c => c.Equals(x.SpliceConstant('o')))
        );

        var expected = ExpressionOnNone.Of(() => "foo".Any(c => c.Equals('o')));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_not_apply_splices_for_nested_interpolation() {
        #pragma warning disable ARB001
        #pragma warning disable ARB004
        var interpolated = ExpressionOnNone.Interpolate(
            x => default(Owner)!.Dogs.AsQueryable()
            .Any(ExpressionOn<Dog>.Interpolate((y, d) => y.SpliceConstant(true)))
        );
        #pragma warning restore

        var expected = ExpressionOnNone.Of(
            () => default(Owner)!.Dogs.AsQueryable()
            .Any(ExpressionOn<Dog>.Interpolate((y, d) => y.SpliceConstant(true)))
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_apply_splices_for_outer_context_in_nested_interpolation() {
        #pragma warning disable ARB001
        #pragma warning disable ARB004
        var interpolated = ExpressionOnNone.Interpolate(
            x => default(Owner)!.Dogs.AsQueryable()
            .Any(ExpressionOn<Dog>.Interpolate((y, d) => x.SpliceConstant(true)))
        );
        #pragma warning restore

        #pragma warning disable ARB001
        var expected = ExpressionOnNone.Of(
            () => default(Owner)!.Dogs.AsQueryable()
            .Any(ExpressionOn<Dog>.Interpolate((y, d) => true))
        );
        #pragma warning restore

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_not_apply_splices_for_shadowing_context_in_nested_interpolation() {
        #pragma warning disable ARB001
        #pragma warning disable ARB004
        var interpolated = ExpressionOnNone.Interpolate(
            x => default(Owner)!.Dogs.AsQueryable()
            .Any(ExpressionOn<Dog>.Interpolate((x, d) => x.SpliceConstant(true)))
        );
        #pragma warning restore

        var expected = ExpressionOnNone.Of(
            () => default(Owner)!.Dogs.AsQueryable()
            .Any(ExpressionOn<Dog>.Interpolate((x, d) => x.SpliceConstant(true)))
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_throw_for_outer_data_access_in_nested_interpolation() {
        Assert.Throws<InterpolationContextReferenceException>(() => {
            #pragma warning disable ARB001
            #pragma warning disable ARB002
            #pragma warning disable ARB004
            _ = ExpressionOnNone.Interpolate(
                default(object),
                x => ExpressionOnNone.Interpolate("bar", y => x.Data)
            );
            #pragma warning restore
        });
    }

    [Fact]
    public void Should_not_throw_for_shadowing_data_access_in_nested_interpolation() {
        #pragma warning disable ARB001
        #pragma warning disable ARB002
        #pragma warning disable ARB004
        _ = ExpressionOnNone.Interpolate(
            default(object),
            x => ExpressionOnNone.Interpolate("bar", x => x.Data)
        );
        #pragma warning restore
    }
}
