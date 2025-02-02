using Arborist.TestFixtures;
using Arborist.Interpolation;
using Arborist.Interpolation.Internal;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_default_expression() {
        var interpolated = ExpressionOn<Owner>.Interpolate(
            new Cat { Id = 42 },
            (x, o) => x.SpliceValue(x.Data.Id) == default
        );

        var ownerParameter = Expression.Parameter(typeof(Owner), "o");

        Assert.Equivalent(
            expected: Expression.Lambda<Func<Owner, bool>>(
                Expression.MakeBinary(
                    ExpressionType.Equal,
                    Expression.Constant(42, typeof(int)),
                    Expression.Constant(default(int), typeof(int))
                ),
                ownerParameter
            ),
            actual: interpolated
        );
    }

    [Fact]
    public void Interpolate_should_throw_InterpolatedParameterCaptureException() {
        var spliceBodyMethod = typeof(IInterpolationContext).GetMethods().Single(m => m.GetParameters().Length == 2);
        var parameters = spliceBodyMethod.GetParameters();

        Assert.True(parameters[0].IsDefined(typeof(InterpolatedSpliceParameterAttribute), false));
        Assert.True(parameters[1].IsDefined(typeof(EvaluatedSpliceParameterAttribute), false));

        Assert.Throws<InterpolatedParameterCaptureException>(() => {
            #pragma warning disable ARB002
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) => x.SpliceBody(o, y => o));
            #pragma warning restore
        });
    }

    [Fact]
    public void Interpolate_should_throw_EvaluatedSpliceException() {
        Assert.Throws<InterpolationContextEvaluationException>(() => {
            #pragma warning disable ARB001
            ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(x.SpliceValue(1) + 2));
            #pragma warning restore
        });
    }

    [Fact]
    public void Interpolate_Splice_should_work_as_expected() {
        var add = Expression.Add(Expression.Constant(1), Expression.Constant(2));
        #pragma warning disable ARB001
        var interpolated = ExpressionOnNone.Interpolate(default(object), x => 2 * x.Splice<int>(add));
        #pragma warning restore

        var expected = Expression.Lambda<Func<int>>(Expression.Multiply(Expression.Constant(2), add));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_as_expected_for_0_parameters() {
        var spliced = ExpressionOnNone.Of(() => "foo");
        #pragma warning disable ARB001
        var interpolated = ExpressionOnNone.Interpolate(default(object), x => x.SpliceBody(spliced).Length);
        #pragma warning restore

        var expected = Expression.Lambda<Func<int>>(
            body: Expression.Property(
                Expression.Constant("foo"),
                typeof(string).GetProperty(nameof(string.Length))!
            )
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_as_expected_for_1_parameter() {
        var nameExpr = ExpressionOn<Owner>.Of(o => o.Name);
        #pragma warning disable ARB001
        var interpolated = ExpressionOn<Owner>.Interpolate(default(object), (x, o) => x.SpliceBody(o, nameExpr).Length);
        #pragma warning restore

        var expected = Expression.Lambda<Func<Owner, int>>(
            body: Expression.Property(
                Expression.Property(
                    interpolated.Parameters[0],
                    typeof(Owner).GetProperty(nameof(Owner.Name))!
                ),
                typeof(string).GetProperty("Length")!
            ),
            parameters: interpolated.Parameters
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_within_a_lambda() {
        var catNameExpr = ExpressionOn<Cat>.Of(c => c.Name);

        #pragma warning disable ARB001
        var interpolated = ExpressionOn<Owner>.Interpolate(default(object),
            (x, o) => o.CatsEnumerable.Any(c => x.SpliceBody(c, catNameExpr) == "Garfield")
        );
        #pragma warning restore

        var compiled = interpolated.Compile();

        Assert.True(compiled(new() { CatsEnumerable = [new() { Name  = "Garfield" }] }));
        Assert.False(compiled(new() { CatsEnumerable = [new() { Name  = "Nermal" }] }));
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_within_a_subexpression() {
        var catNameExpr = ExpressionOn<Cat>.Of(c => c.Name);
        #pragma warning disable ARB001
        var interpolated = ExpressionOn<Owner>.Interpolate(default(object),
            (x, o) => o.CatsQueryable.Any(c => x.SpliceBody(c, catNameExpr) == "Garfield")
        );
        #pragma warning restore

        var catParameter = Expression.Parameter(typeof(Cat), "c");
        var expected = Expression.Lambda<Func<Owner, bool>>(
            Expression.Call(
                typeof(Queryable).GetMethods()
                .Single(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(Cat)),
                Expression.Property(
                    interpolated.Parameters[0],
                    typeof(Owner).GetProperty(nameof(Owner.CatsQueryable))!
                ),
                Expression.Lambda<Func<Cat, bool>>(
                    Expression.Equal(
                        Expression.Property(
                            catParameter,
                            typeof(Cat).GetProperty(nameof(Cat.Name))!
                        ),
                        Expression.Constant("Garfield")
                    ),
                    catParameter
                )
            ),
            interpolated.Parameters
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceQuoted_should_work_as_expected() {
        var quoted = Expression.Lambda<Func<Cat, bool>>(
            Expression.Constant(true),
            Expression.Parameter(typeof(Cat))
        );

        var interpolated = ExpressionOn<Owner>.Interpolate(
            new { quoted },
            static (x, o) => o.CatsQueryable.Any(x.SpliceQuoted(x.Data.quoted))
        );

        var expected = Expression.Lambda<Func<Owner, bool>>(
            Expression.Call(
                typeof(Queryable).GetMethods()
                .Single(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(Cat)),
                Expression.Property(
                    interpolated.Parameters[0],
                    typeof(Owner).GetProperty(nameof(Owner.CatsQueryable))!
                ),
                Expression.Quote(quoted)
            ),
            interpolated.Parameters
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceValue_should_embed_constants() {
        var interpolated = ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue("foo"));

        var expected = Expression.Lambda<Func<string>>(Expression.Constant("foo"));

        Assert.Equivalent(expected, interpolated);
    }
}
