using Arborist.Fixtures;

namespace Arborist;

public class GraftTests {
    [Fact]
    public void Graft0_works_as_expected() {
        var expected = Expression.Lambda<Func<int>>(
            Expression.Property(
                Expression.Constant("foo"),
                typeof(string).GetProperty(nameof(string.Length))!
            )
        );

        var actual = ExpressionOnNone.Graft(() => "foo", str => str.Length);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void Graft1_works_as_expected() {
        var actual = ExpressionOn<Cat>.Graft(c => c.Name, str => str.Length);

        var expected = Expression.Lambda<Func<Cat, int>>(
            Expression.Property(
                Expression.Property(
                    actual.Parameters[0],
                    typeof(Cat).GetProperty(nameof(Cat.Name))!
                ),
                typeof(string).GetProperty(nameof(string.Length))!
            ),
            actual.Parameters
        );

        Assert.Equivalent(expected, actual);
    }
}
