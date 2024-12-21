using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_implicit_boxing_conversion() {
        var interpolated = ExpressionOn<Owner>.Interpolate<Cat, object>(
            new Cat { Id = 42 },
            (x, o) => x.SpliceValue(x.Data.Id)
        );

        Assert.Equivalent(
            expected: Expression.Lambda<Func<Owner, object>>(
                Expression.Convert(
                    Expression.Constant(42, typeof(int)),
                    typeof(object)
                ),
                Expression.Parameter(typeof(Owner), "o")
            ),
            actual: interpolated
        );
    }

    [Fact]
    public void Should_handle_implicit_numeric_conversion() {
        var expr = ExpressionOn<Owner>.Interpolate<Cat, decimal>(new Cat { Id = 42 }, (x, o) => x.SpliceValue(x.Data.Id));

        var parameter = Expression.Parameter(typeof(Owner), "o");

        Assert.Equivalent(
            expected: Expression.Lambda<Func<Owner, decimal>>(
                Expression.Convert(
                    Expression.Constant(42, typeof(int)),
                    typeof(decimal)
                ),
                parameter
            ),
            actual: expr
        );
    }

    [Fact]
    public void Should_handle_implicit_user_defined_conversion() {
        var interpolated = ExpressionOn<Owner>.Interpolate<Cat, ImplicitlyConvertible<string>>(
            new Cat { Name = "Garfield" },
            (x, o) => x.SpliceValue(x.Data.Name)
        );

        Assert.Equivalent(
            expected: Expression.Lambda<Func<Owner, ImplicitlyConvertible<string>>>(
                Expression.Convert(
                    Expression.Constant("Garfield"),
                    typeof(ImplicitlyConvertible<string>)
                ),
                Expression.Parameter(typeof(Owner), "o")
            ),
            actual: interpolated
        );
    }
}
