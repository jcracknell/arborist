using Arborist.TestFixtures;

namespace Arborist.Orderings;

using OwnerOrdering = Ordering<Expression<Func<Owner, object?>>>;

public class OrderingExtensionTests {
    private abstract class CatOrderingSelector : IOrderingSelector<CatOrderingSelector> {
        private CatOrderingSelector() { }

        public abstract bool IsAbsoluteOrdering { get; }
        public abstract override int GetHashCode();
        public abstract bool Equals(CatOrderingSelector? other);

        public override bool Equals(object? obj) =>
            Equals(obj as CatOrderingSelector);

        public sealed class Id : CatOrderingSelector {
            public override bool IsAbsoluteOrdering => true;
            public override int GetHashCode() => nameof(Id).GetHashCode();
            public override bool Equals(CatOrderingSelector? other) => other is Id;
        }

        public sealed class Name : CatOrderingSelector {
            public override bool IsAbsoluteOrdering => false;
            public override int GetHashCode() => nameof(Name).GetHashCode();
            public override bool Equals(CatOrderingSelector? other) => other is Name;
        }

        public sealed class Age : CatOrderingSelector {
            public override bool IsAbsoluteOrdering => false;
            public override int GetHashCode() => nameof(Age).GetHashCode();
            public override bool Equals(CatOrderingSelector? other) => other is Age;
        }
    }

    [Fact]
    public void GraftSelectorExpressionsTo_should_work_as_expected() {
        var actual = OwnerOrdering.ByAscending(o => o.Name).ThenByDescending(o => o.Id)
        .GraftSelectorExpressionsTo(ExpressionOn<Cat>.Of(c => c.Owner));

        Assert.Equal(2, actual.Count());
        Assert.Equivalent(OrderingTerm.Ascending(ExpressionOn<Cat>.Of<object?>(c => c.Owner.Name)), actual.ElementAt(0));
        Assert.Equivalent(OrderingTerm.Descending(ExpressionOn<Cat>.Of<object?>(c => c.Owner.Id)), actual.ElementAt(1));
    }

    [Fact]
    public void GraftSelectorExpressionsTo_should_work_as_expected_for_intermediate_struct() {
        var actual = Ordering<Expression<Func<int, object?>>>.ByDescending(i => i % 2 == 0)
        .ThenByAscending(i => i == 2)
        .GraftSelectorExpressionsTo(ExpressionOn<Owner>.Of(o => o.Id));

        Assert.Equal(2, actual.Count());
        Assert.Equivalent(OrderingTerm.Descending(ExpressionOn<Owner>.Of<object?>(o => o.Id % 2 == 0)), actual.ElementAt(0));
        Assert.Equivalent(OrderingTerm.Ascending(ExpressionOn<Owner>.Of<object?>(o => o.Id == 2)), actual.ElementAt(1));
    }

    [Fact]
    public void GraftSelectorExpressionToNullable_should_work_for_intermediate_class_and_result_class() {
        var expected = ExpressionOn<Cat>.Of<object?>(c => c.Owner != null ? c.Owner.Name : null);

        var actual = Ordering<Expression<Func<Owner, object?>>>.ByAscending(o => o.Name)
        .GraftSelectorExpressionsToNullable(ExpressionOn<Cat>.Of(c => c.Owner));

        Assert.Equal(1, actual.Count());
        Assert.Equivalent(expected, actual.ElementAt(0).Selector);
    }

    [Fact]
    public void GraftSelectorExpressionToNullable_should_work_for_intermediate_class_and_result_struct() {
        var actual = Ordering<Expression<Func<Owner, int>>>.ByAscending(o => o.Id)
        .GraftSelectorExpressionsToNullable(ExpressionOn<Cat>.Of(c => c.Owner));

        Assert.Equal(1, actual.Count());
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of<int?>(c => c.Owner != null ? c.Owner.Id : null),
            actual: actual.ElementAt(0).Selector
        );
    }

    [Fact]
    public void GraftSelectorExpressionToNullable_should_work_for_intermediate_class_and_result_nullable() {
        var actual = Ordering<Expression<Func<Owner, int?>>>.ByAscending(o => new int?(o.Id))
        .GraftSelectorExpressionsToNullable(ExpressionOn<Cat>.Of(c => c.Owner));

        Assert.Equal(1, actual.Count());
        Assert.Equivalent(
            expected: ExpressionOn<Cat>.Of<int?>(c => c.Owner != null ? new int?(c.Owner.Id) : null),
            actual: actual.ElementAt(0).Selector
        );
    }

    [Fact]
    public void GraftSelectorExpressionToNullable_should_work_for_intermediate_nullable_and_result_class() {
        var actual = Ordering<Expression<Func<int, object?>>>.ByAscending(i => i.ToString())
        .GraftSelectorExpressionsToNullable(ExpressionOn<Cat>.Of(c => new int?(c.Id)));

        Assert.Equal(1, actual.Count());
        Assert.Equivalent(
            #pragma warning disable CS0472
            expected: ExpressionOn<Cat>.Of<object?>(c => new int?(c.Id) != null ? new int?(c.Id)!.Value.ToString() : null),
            #pragma warning restore CS0472
            actual: actual.ElementAt(0).Selector
        );
    }

    [Fact]
    public void GraftSelectorExpressionToNullable_should_work_for_intermediate_nullable_and_result_struct() {
        var actual = Ordering<Expression<Func<int, int>>>.ByAscending(i => i / 2)
        .GraftSelectorExpressionsToNullable(ExpressionOn<Cat>.Of(c => new int?(c.Id)));

        Assert.Equal(1, actual.Count());
        Assert.Equivalent(
            #pragma warning disable CS0472
            expected: ExpressionOn<Cat>.Of<int?>(c => new int?(c.Id) != null ? new int?(c.Id)!.Value / 2 : null),
            #pragma warning restore CS0472
            actual: actual.ElementAt(0).Selector
        );
    }

    [Fact]
    public void GraftSelectorExpressionToNullable_should_work_for_intermediate_nullable_and_result_nullable() {
        var actual = Ordering<Expression<Func<int, int?>>>.ByAscending(i => new int?(i / 2))
        .GraftSelectorExpressionsToNullable(ExpressionOn<Cat>.Of(c => new int?(c.Id)));

        Assert.Equal(1, actual.Count());
        Assert.Equivalent(
            #pragma warning disable CS0472
            expected: ExpressionOn<Cat>.Of<int?>(c => new int?(c.Id) != null ? new int?(new int?(c.Id)!.Value / 2) : null),
            #pragma warning restore CS0472
            actual: actual.ElementAt(0).Selector
        );
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
    public void Simplify_should_drop_terms_with_duplicated_selectors() {
        var expected = Ordering<int>.By([
            OrderingTerm.Ascending(1),
            OrderingTerm.Descending(2),
            OrderingTerm.Descending(3)
        ]);

        var actual = Ordering<int>.By([
            OrderingTerm.Ascending(1),
            OrderingTerm.Descending(2),
            OrderingTerm.Descending(1),
            OrderingTerm.Descending(3)
        ])
        .Simplify();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Simplify_should_drop_terms_following_absolute_selector() {
        var expected = Ordering<CatOrderingSelector>.By(
            OrderingTerm.Ascending(new CatOrderingSelector.Name()),
            OrderingTerm.Descending(new CatOrderingSelector.Id())
        );

        var actual = Ordering<CatOrderingSelector>.By(
            OrderingTerm.Ascending(new CatOrderingSelector.Name()),
            OrderingTerm.Descending(new CatOrderingSelector.Id()),
            OrderingTerm.Descending(new CatOrderingSelector.Age())
        )
        .Simplify();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Take_should_return_the_expected_terms() {
        var ordering = Ordering<CatOrderingSelector>.By(
            OrderingTerm.Ascending(new CatOrderingSelector.Name()),
            OrderingTerm.Descending(new CatOrderingSelector.Id()),
            OrderingTerm.Descending(new CatOrderingSelector.Age())
        );

        var expected = Ordering<CatOrderingSelector>.By(
            OrderingTerm.Ascending(new CatOrderingSelector.Name()),
            OrderingTerm.Descending(new CatOrderingSelector.Id())
        );

        Assert.Equal(expected, ordering.Take(2));
    }

    [Fact]
    public void Take_should_return_the_expected_number_of_terms() {
        var ordering = Ordering<CatOrderingSelector>.By(
            OrderingTerm.Ascending(new CatOrderingSelector.Name()),
            OrderingTerm.Descending(new CatOrderingSelector.Id()),
            OrderingTerm.Descending(new CatOrderingSelector.Age())
        );

        Assert.Equal(0, ordering.Take(0).Count());
        Assert.Equal(1, ordering.Take(1).Count());
        Assert.Equal(2, ordering.Take(2).Count());
    }

    [Fact]
    public void Take_should_return_the_empty_ordering_for_negative_count() {
        var ordering = Ordering<CatOrderingSelector>.By(
            OrderingTerm.Ascending(new CatOrderingSelector.Name()),
            OrderingTerm.Descending(new CatOrderingSelector.Id()),
            OrderingTerm.Descending(new CatOrderingSelector.Age())
        );

        Assert.Equal(
            expected: Ordering<CatOrderingSelector>.Unordered,
            actual: ordering.Take(-1)
        );
    }
}
