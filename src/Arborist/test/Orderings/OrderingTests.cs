namespace Arborist.Orderings;

public class OrderingTests {
    [Fact]
    public void CollectionBuilderAttribute_should_work_as_expected() {
        var expected = Ordering.By(OrderingTerm.Create("foo", OrderingDirection.Ascending));
        Ordering<string> actual = [OrderingTerm.Create("foo", OrderingDirection.Ascending)];
        Assert.Equivalent(expected, actual, strict: true);
    }

    [Fact]
    public void Unordered_should_be_empty() {
        Assert.True(Ordering<string>.Unordered.IsEmpty);
        Assert.False(Ordering<string>.Unordered.Any());
    }

    [Fact]
    public void ApplyDirection_should_work_as_expected() {
        var ordering = Ordering.By(OrderingTerm.Ascending("foo"), OrderingTerm.Descending("bar"));

        Assert.Equivalent(ordering, ordering.ApplyDirection(OrderingDirection.Ascending), strict: true);
        Assert.Equivalent(ordering.Invert(), ordering.ApplyDirection(OrderingDirection.Descending), strict: true);
    }

    [Fact]
    public void By_term_should_work_as_expected() {
        var actual = Ordering.By(OrderingTerm.Create("a", OrderingDirection.Ascending));
        Assert.Equal(1, actual.Count());
        Assert.Equivalent(OrderingTerm.Create("a", OrderingDirection.Ascending), actual.ElementAt(0));
    }

    [Fact]
    public void By_enumerable_should_work_as_expected() {
        var actual = Ordering.By(new[] { OrderingTerm.Ascending("a"), OrderingTerm.Descending("b") });

        Assert.Equal(2, actual.Count());
        Assert.Equivalent(OrderingTerm.Ascending("a"), actual.ElementAt(0));
        Assert.Equivalent(OrderingTerm.Descending("b"), actual.ElementAt(1));
    }

    [Fact]
    public void By_enumerable_should_return_Unordered_instance_for_empty() {
        var actual = Ordering.By(Enumerable.Empty<OrderingTerm<string>>());
        Assert.Same(Ordering<string>.Unordered, actual);
    }

    [Fact]
    public void By_should_work_with_collection_expression() {
        var actual = Ordering.By([OrderingTerm.Ascending("a"), OrderingTerm.Descending("b")]);

        Assert.Equal(2, actual.Count());
        Assert.Equal(OrderingTerm.Ascending("a"), actual.ElementAt(0));
        Assert.Equal(OrderingTerm.Descending("b"), actual.ElementAt(1));
    }

    [Fact]
    public void By_should_work_with_params() {
        var actual = Ordering.By(OrderingTerm.Ascending("a"), OrderingTerm.Descending("b"));

        Assert.Equal(2, actual.Count());
        Assert.Equal(OrderingTerm.Ascending("a"), actual.ElementAt(0));
        Assert.Equal(OrderingTerm.Descending("b"), actual.ElementAt(1));
    }

    [Fact]
    public void ByAscending_should_work_as_expected() {
        var actual = Ordering.ByAscending("foo");

        Assert.Equal(1, actual.Count());
        Assert.Equal(OrderingTerm.Ascending("foo"), actual.ElementAt(0));
    }

    [Fact]
    public void ByDescending_should_work_as_expected() {
        var actual = Ordering.ByDescending("foo");

        Assert.Equal(1, actual.Count());
        Assert.Equal(OrderingTerm.Descending("foo"), actual.ElementAt(0));
    }

    [Fact]
    public void Invert_should_work_as_expected() {
        var actual = Ordering.By(OrderingTerm.Descending("foo"), OrderingTerm.Ascending("bar")).Invert();

        Assert.Equal(2, actual.Count());
        Assert.Equal(OrderingTerm.Ascending("foo"), actual.ElementAt(0));
        Assert.Equal(OrderingTerm.Descending("bar"), actual.ElementAt(1));
    }

    [Fact]
    public void Select_should_work_as_expected() {
        var actual = Ordering.ByAscending("a").ThenByDescending("aa")
        .Select(term => OrderingTerm.Create(term.Selector.Length, term.Direction));

        Assert.Equal(2, actual.Count());
        Assert.Equivalent(OrderingTerm.Create(1, OrderingDirection.Ascending), actual.ElementAt(0));
        Assert.Equivalent(OrderingTerm.Create(2, OrderingDirection.Descending), actual.ElementAt(1));
    }

    [Fact]
    public void SelectMany_should_work_as_expected() {
        var actual = Ordering.ByAscending("ab").ThenByDescending("cd")
        .SelectMany(term => term.Selector.Select(c => OrderingTerm.Create(c, term.Direction)));

        Assert.IsAssignableFrom<Ordering<char>>(actual);
        Assert.Equal(4, actual.Count());
        Assert.Equivalent(OrderingTerm.Ascending('a'), actual.ElementAt(0));
        Assert.Equivalent(OrderingTerm.Ascending('b'), actual.ElementAt(1));
        Assert.Equivalent(OrderingTerm.Descending('c'), actual.ElementAt(2));
        Assert.Equivalent(OrderingTerm.Descending('d'), actual.ElementAt(3));
    }

    [Fact]
    public void ThenBy_term_should_work_as_expected() {
        var expected = new[] { OrderingTerm.Ascending("foo"), OrderingTerm.Descending("bar") };
        var actual = Ordering.By("foo", OrderingDirection.Ascending).ThenBy("bar", OrderingDirection.Descending);

        Assert.Equivalent(expected[0], actual.ElementAt(0));
        Assert.Equivalent(expected[1], actual.ElementAt(1));
    }

    [Fact]
    public void ThenBy_enumerable_should_work_as_expected() {
        var expected = new[] { OrderingTerm.Ascending("a"), OrderingTerm.Descending("b"), OrderingTerm.Ascending("c") };
        var actual = Ordering.ByAscending("a").ThenBy(new[] { OrderingTerm.Descending("b"), OrderingTerm.Ascending("c") });

        Assert.Equal(3, actual.Count());
        Assert.Equivalent(expected[0], actual.ElementAt(0));
        Assert.Equivalent(expected[1], actual.ElementAt(1));
        Assert.Equivalent(expected[2], actual.ElementAt(2));
    }

    [Fact]
    public void ThenBy_should_work_with_collection_expression() {
        var actual = Ordering<string>.Unordered.ThenBy([OrderingTerm.Ascending("a"), OrderingTerm.Descending("b")]);

        Assert.Equal(2, actual.Count());
        Assert.Equal(OrderingTerm.Ascending("a"), actual.ElementAt(0));
        Assert.Equal(OrderingTerm.Descending("b"), actual.ElementAt(1));
    }

    [Fact]
    public void ThenBy_should_work_with_params() {
        var actual = Ordering<string>.Unordered.ThenBy(OrderingTerm.Ascending("a"), OrderingTerm.Descending("b"));

        Assert.Equal(2, actual.Count());
        Assert.Equal(OrderingTerm.Ascending("a"), actual.ElementAt(0));
        Assert.Equal(OrderingTerm.Descending("b"), actual.ElementAt(1));
    }

    [Fact]
    public void TranslateSelectors_should_work_as_expected() {
        var actual = Ordering.ByDescending("ab").ThenByAscending("cd").TranslateSelectors(
            s => s.Select((c, i) => OrderingTerm.Create(c, (i % 2) switch {
                0 => OrderingDirection.Ascending,
                _ => OrderingDirection.Descending
            }))
        );

        Assert.Equal(4, actual.Count());
        Assert.Equal(OrderingTerm.Descending('a'), actual.ElementAt(0));
        Assert.Equal(OrderingTerm.Ascending('b'), actual.ElementAt(1));
        Assert.Equal(OrderingTerm.Ascending('c'), actual.ElementAt(2));
        Assert.Equal(OrderingTerm.Descending('d'), actual.ElementAt(3));
    }

    [Fact]
    public void Where_should_work_as_expected() {
        var actual = Ordering.ByAscending("a").ThenByDescending("aa").ThenByAscending("aaa")
        .Where(term => term.Selector.Length % 2 == 1);

        Assert.Equal(2, actual.Count());
        Assert.Equivalent(OrderingTerm.Ascending("a"), actual.ElementAt(0));
        Assert.Equivalent(OrderingTerm.Ascending("aaa"), actual.ElementAt(1));
    }
}
