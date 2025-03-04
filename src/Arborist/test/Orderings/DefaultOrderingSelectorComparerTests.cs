namespace Arborist.Orderings;

public class DefaultOrderingSelectorComparerTests {
    private abstract class CatOrderingSelector : IOrderingSelector<CatOrderingSelector> {
        private CatOrderingSelector() { }

        public abstract bool IsAbsoluteOrdering { get; }
        public abstract override int GetHashCode();
        public abstract bool Equals(CatOrderingSelector? that);

        public override bool Equals(object? obj) =>
            Equals(obj as CatOrderingSelector);

        public sealed class Id : CatOrderingSelector {
            public Id() : this(true) { }

            public Id(bool isAbsoluteOrdering) {
                IsAbsoluteOrdering = isAbsoluteOrdering;
            }

            public override bool IsAbsoluteOrdering { get; }
            public override int GetHashCode() => nameof(Id).GetHashCode();
            public override bool Equals(CatOrderingSelector? that) => that is Id;
        }

        public sealed class Name : CatOrderingSelector {
            public override bool IsAbsoluteOrdering { get; }
            public override int GetHashCode() => nameof(Name).GetHashCode();
            public override bool Equals(CatOrderingSelector? that) => that is Name;
        }
    }

    private sealed class CatOrderingSelectorComparer : IOrderingSelectorComparer<CatOrderingSelector> {
        public bool IsAbsoluteOrdering(CatOrderingSelector selector) =>
            selector is CatOrderingSelector.Id;

        public bool Equals(CatOrderingSelector? x, CatOrderingSelector? y) =>
            x?.GetType() == y?.GetType();

        public int GetHashCode(CatOrderingSelector obj) =>
            obj.GetType().GetHashCode();
    }

    [Fact]
    public void GetHashCode_should_throw_ArgumentNullException() {
        var comparer = new DefaultOrderingSelectorComparer<CatOrderingSelector>(
            EqualityComparer<CatOrderingSelector>.Default
        );

        Assert.ThrowsAny<ArgumentNullException>(() => {
            _ = comparer.GetHashCode(null!);
        });
    }

    [Fact]
    public void IsAbsoluteOrdering_should_throw_ArgumentNullException() {
        var comparer = new DefaultOrderingSelectorComparer<CatOrderingSelector>(
            EqualityComparer<CatOrderingSelector>.Default
        );

        Assert.ThrowsAny<ArgumentNullException>(() => {
            comparer.IsAbsoluteOrdering(null!);
        });
    }

    [Fact]
    public void IsAbsoluteOrdering_should_defer_to_IOrderingSelector_implementation() {
        var comparer = new DefaultOrderingSelectorComparer<CatOrderingSelector>(
            EqualityComparer<CatOrderingSelector>.Default
        );

        Assert.True(comparer.IsAbsoluteOrdering(new CatOrderingSelector.Id()));
        Assert.False(comparer.IsAbsoluteOrdering(new CatOrderingSelector.Name()));
    }

    [Fact]
    public void IsAbsoluteOrdering_should_defer_to_IOrderingSelectorComparer_implementation_provided_as_IEqualityComparer() {
        var comparer = new DefaultOrderingSelectorComparer<CatOrderingSelector>(
            new CatOrderingSelectorComparer()
        );

        Assert.True(comparer.IsAbsoluteOrdering(new CatOrderingSelector.Id()));
        Assert.False(comparer.IsAbsoluteOrdering(new CatOrderingSelector.Name()));
    }

    [Fact]
    public void IsAbsoluteOrdering_should_defer_to_IOrderingSelectorComparer_implementation_over_IOrderingSelector() {
        var comparer = new DefaultOrderingSelectorComparer<CatOrderingSelector>(
            new CatOrderingSelectorComparer()
        );

        var selector = new CatOrderingSelector.Id(isAbsoluteOrdering: false);

        Assert.False(selector.IsAbsoluteOrdering);
        Assert.True(comparer.IsAbsoluteOrdering(selector));
    }
}
