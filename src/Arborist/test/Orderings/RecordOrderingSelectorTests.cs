namespace Arborist.Orderings;

public class RecordOrderingSelectorTests {
    private abstract record CatOrderingSelector
        : RecordOrderingSelector<CatOrderingSelector>
    {
        // Private constructor prevents subclassing elsewhere
        private CatOrderingSelector() { }

        public sealed record Id : CatOrderingSelector {
            protected override bool IsAbsoluteOrdering => true;
        }

        public sealed record Name : CatOrderingSelector;

        public sealed record Param(string Value) : CatOrderingSelector;
    }

    [Fact]
    public void Equals_should_work_as_expected() {
        Assert.True(new CatOrderingSelector.Id().Equals((object)new CatOrderingSelector.Id()));
        Assert.False(new CatOrderingSelector.Id().Equals((object)new CatOrderingSelector.Name()));
        Assert.True(new CatOrderingSelector.Id().Equals((CatOrderingSelector)new CatOrderingSelector.Id()));
        Assert.False(new CatOrderingSelector.Id().Equals((CatOrderingSelector)new CatOrderingSelector.Name()));
        Assert.True(new CatOrderingSelector.Param("foo").Equals(new CatOrderingSelector.Param("foo")));
        Assert.False(new CatOrderingSelector.Param("foo").Equals(new CatOrderingSelector.Param("bar")));
    }

    [Fact]
    public void IsAbsoluteOrdering_should_work_as_expected() {
        Assert.False(((IOrderingSelectorLike)new CatOrderingSelector.Name()).IsAbsoluteOrdering);
        Assert.True(((IOrderingSelectorLike)new CatOrderingSelector.Id()).IsAbsoluteOrdering);
    }
}
