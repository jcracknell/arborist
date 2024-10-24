namespace Arborist;

public partial class ExpressionThunkTests {
    [Fact]
    public void Rebase_works_as_expected() {
        var expected = Expression.Lambda<Func<int>>(
            Expression.Property(
                Expression.Constant("foo"),
                typeof(string).GetProperty(nameof(string.Length))!
            )
        );

        var actual = ExpressionThunk.Rebase(() => "foo", str => str.Length);

        Assert.Equivalent(expected, actual);
    }
}
