using Arborist.Fixtures;

namespace Arborist;

public partial class ExpressionOn1Tests {
    [Fact]
    public void Rebase_works_as_expected() {
        var actual = ExpressionOn<Cat>.Rebase(c => c.Name, str => str.Length);

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
