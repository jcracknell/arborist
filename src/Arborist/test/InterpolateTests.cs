using Arborist.Fixtures;
using Arborist.Interpolation;

namespace Arborist;

public class InterpolateTests {
    [Fact]
    public void Interpolate_should_throw_InterpolatedParameterCaptureException() {
        var spliceBodyMethod = typeof(IInterpolationContext).GetMethods().Single(m => m.GetParameters().Length == 2);
        var parameters = spliceBodyMethod.GetParameters();

        Assert.True(parameters[0].IsDefined(typeof(InterpolatedSpliceParameterAttribute), false));
        Assert.True(parameters[1].IsDefined(typeof(EvaluatedSpliceParameterAttribute), false));

        Assert.Throws<InterpolatedParameterCaptureException>(() => {
            ExpressionOn<Owner>.Interpolate((x, o) => x.SpliceBody(o, y => o));
        });
    }

    [Fact]
    public void Interpolate_should_throw_EvaluatedSpliceException() {
        Assert.Throws<InterpolationContextEvaluationException>(() => {
            ExpressionOnNone.Interpolate(x => x.SpliceValue(x.SpliceValue(1) + 2));
        });
    }

    [Fact]
    public void Interpolate_Splice_should_work_as_expected() {
        var add = Expression.Add(Expression.Constant(1), Expression.Constant(2));
        var interpolated = ExpressionOnNone.Interpolate(x => 2 * x.Splice<int>(add));

        var expected = Expression.Lambda<Func<int>>(Expression.Multiply(Expression.Constant(2), add));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_as_expected_for_0_parameters() {
        var spliced = ExpressionOnNone.Of(() => "foo");
        var interpolated = ExpressionOnNone.Interpolate(x => x.SpliceBody(spliced).Length);

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
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) => x.SpliceBody(o, nameExpr).Length);

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
        var interpolated = ExpressionOn<Owner>.Interpolate(
            (x, o) => o.CatsEnumerable.Any(c => x.SpliceBody(c, catNameExpr) == "Garfield")
        );

        var compiled = interpolated.Compile();

        Assert.True(compiled(new() { CatsEnumerable = [new() { Name  = "Garfield" }] }));
        Assert.False(compiled(new() { CatsEnumerable = [new() { Name  = "Nermal" }] }));
    }

    [Fact]
    public void Interpolate_SpliceBody_should_work_within_a_subexpression() {
        var catNameExpr = ExpressionOn<Cat>.Of(c => c.Name);
        var interpolated = ExpressionOn<Owner>.Interpolate(
            (x, o) => o.CatsQueryable.Any(c => x.SpliceBody(c, catNameExpr) == "Garfield")
        );

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

        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) => o.CatsQueryable.Any(x.SpliceQuoted(quoted)));

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
        var interpolated = ExpressionOnNone.Interpolate(x => x.SpliceValue("foo"));

        var expected = Expression.Lambda<Func<string>>(Expression.Constant("foo"));

        Assert.Equivalent(expected, interpolated);
    }
}
