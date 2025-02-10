using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Splice_should_work_with_expression() {
        var data = new {
            Addition = Expression.Add(Expression.Constant(1), Expression.Constant(2))
        };
        
        var interpolated = InterpolationTestOnNone.Interpolate(
            data,
            x => 2 * x.Splice<int>(x.Data.Addition)
        );

        var expected = Expression.Lambda<Func<int>>(
            Expression.Multiply(Expression.Constant(2), data.Addition)
        );

        Assert.Equivalent(expected, interpolated);
    }
    
    [Fact]
    public void Splice_should_work_with_expression1() {
        var interpolated = ExpressionOn<Owner>.Interpolate(
            new { Predicate = ExpressionOn<Cat>.Of(c => c.Age == 8) },
            (x, o) => o.CatsEnumerable.Any(x.Splice(x.Data.Predicate))
        );
        
        var expected = ExpressionOn<Owner>.Of(o => o.CatsEnumerable.Any(c => c.Age == 8));
        
        Assert.Equivalent(expected, interpolated);
    }
}
