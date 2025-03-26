using Arborist.TestFixtures;

namespace Arborist;

public class QueryableInterpolationExtensionsTests {
    // We need to validate that all of the generated extensions are unambiguously resolvable by the
    // compiler. This requires some special handling in the source generator for:
    //
    //   - overloads accepting additional expression parameters which could conflict with the
    //     interpolation context (Select, SelectMany); and
    //   - overloads accepting a generic parameter which would conflict with the interpolation data
    //     (Aggregate).

    private static IQueryable<Cat> CatQueryable =>
        throw new NotImplementedException();

    private static IQueryable<int> IntQueryable =>
        throw new NotImplementedException();

    private static IQueryable<Owner> OwnerQueryable =>
        throw new NotImplementedException();

    private static void Resolves(Action action) { }

    [Fact]
    public void AggregateInterpolated_should_resolve() {
        Resolves(() => {
            IntQueryable.AggregateInterpolated((x, acc, i) => acc + i + x.SpliceConstant(42));
            OwnerQueryable.AggregateInterpolated(0, (x, acc, o) => acc + o.Id + x.SpliceConstant(42));
            OwnerQueryable.AggregateInterpolated(0, (acc, o) => acc + o.Id, (x, v) => x.SpliceConstant(42));
            OwnerQueryable.AggregateInterpolated(0, (x, acc, o) => acc + o.Id + x.SpliceConstant(42), v => v);
            OwnerQueryable.AggregateInterpolated(0, (x, acc, o) => acc + o.Id + x.SpliceConstant(42), (x, v) => x.SpliceConstant(42));
        });
    }

    #if NET9_0_OR_GREATER
    [Fact]
    public void AggregateByInterpolated_should_resolve() {
        var data = new { Int = 42, Str = "foo" };

        Resolves(() => {
            OwnerQueryable.AggregateByInterpolated((x, o) => x.SpliceConstant(42), (x, k) => x.SpliceConstant("foo"), (x, acc, o) => acc + x.SpliceConstant("foo"));
            OwnerQueryable.AggregateByInterpolated((x, o) => x.SpliceConstant(42), (x, k) => x.SpliceConstant("foo"), (x, acc, o) => acc + x.SpliceConstant("foo"), EqualityComparer<int>.Default);
            OwnerQueryable.AggregateByInterpolated(data, (x, o) => x.SpliceConstant(x.Data.Int), (x, k) => x.SpliceConstant(x.Data.Str), (x, acc, o) => acc + x.SpliceConstant(x.Data.Str));
            OwnerQueryable.AggregateByInterpolated(data, (x, o) => x.SpliceConstant(x.Data.Int), (x, k) => x.SpliceConstant(x.Data.Str), (x, acc, o) => acc + x.SpliceConstant(x.Data.Str), EqualityComparer<int>.Default);
            OwnerQueryable.AggregateByInterpolated(o => 42, (x, k) => x.SpliceConstant("foo"), (x, acc, o) => acc + x.SpliceConstant("foo"));
            OwnerQueryable.AggregateByInterpolated(o => 42, (x, k) => x.SpliceConstant("foo"), (x, acc, o) => acc + x.SpliceConstant("foo"), EqualityComparer<int>.Default);
            OwnerQueryable.AggregateByInterpolated(data, o => 42, (x, k) => x.SpliceConstant(x.Data.Str), (x, acc, o) => acc + x.SpliceConstant(x.Data.Str));
            OwnerQueryable.AggregateByInterpolated(data, o => 42, (x, k) => x.SpliceConstant(x.Data.Str), (x, acc, o) => acc + x.SpliceConstant(x.Data.Str), EqualityComparer<int>.Default);
            OwnerQueryable.AggregateByInterpolated((x, o) => x.SpliceConstant(42), k => "foo", (x, acc, o) => acc + x.SpliceConstant("foo"));
            OwnerQueryable.AggregateByInterpolated((x, o) => x.SpliceConstant(42), k => "foo", (x, acc, o) => acc + x.SpliceConstant("foo"), EqualityComparer<int>.Default);
            OwnerQueryable.AggregateByInterpolated(data, (x, o) => x.SpliceConstant(x.Data.Int), k => "foo", (x, acc, o) => acc + x.SpliceConstant(x.Data.Str));
            OwnerQueryable.AggregateByInterpolated(data, (x, o) => x.SpliceConstant(x.Data.Int), k => "foo", (x, acc, o) => acc + x.SpliceConstant(x.Data.Str), EqualityComparer<int>.Default);
            OwnerQueryable.AggregateByInterpolated((x, o) => x.SpliceConstant(42), (x, k) => x.SpliceConstant("foo"), (acc, o) => acc + "foo");
            OwnerQueryable.AggregateByInterpolated((x, o) => x.SpliceConstant(42), (x, k) => x.SpliceConstant("foo"), (acc, o) => acc + "foo", EqualityComparer<int>.Default);
            OwnerQueryable.AggregateByInterpolated(data, (x, o) => x.SpliceConstant(x.Data.Int), (x, k) => x.SpliceConstant(x.Data.Str), (acc, o) => acc + "foo");
            OwnerQueryable.AggregateByInterpolated(data, (x, o) => x.SpliceConstant(x.Data.Int), (x, k) => x.SpliceConstant(x.Data.Str), (acc, o) => acc + "foo", EqualityComparer<int>.Default);
        });
    }
    #endif

    [Fact]
    public void AllInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.AllInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.AllInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void AnyInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.AnyInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.AnyInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void AverageInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.AverageInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.AverageInterpolated((x, o) => x.SpliceConstant(42L));
            OwnerQueryable.AverageInterpolated((x, o) => x.SpliceConstant(42D));
            OwnerQueryable.AverageInterpolated((x, o) => x.SpliceConstant(42F));
            OwnerQueryable.AverageInterpolated((x, o) => x.SpliceConstant(42M));
            OwnerQueryable.AverageInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.AverageInterpolated(42L, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.AverageInterpolated(42D, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.AverageInterpolated(42F, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.AverageInterpolated(42M, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void CountInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.CountInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.CountInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    #if NET9_0_OR_GREATER
    [Fact]
    public void CountByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.CountByInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.CountByInterpolated((x, o) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.CountByInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.CountByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
        });
    }
    #endif

    [Fact]
    public void DistinctByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.DistinctByInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.DistinctByInterpolated((x, o) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.DistinctByInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.DistinctByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
        });
    }

    [Fact]
    public void ExceptByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.ExceptByInterpolated(CatQueryable, (x, o) => x.SpliceConstant(default(Cat)!));
            OwnerQueryable.ExceptByInterpolated(CatQueryable, (x, o) => x.SpliceConstant(default(Cat)!), EqualityComparer<Cat>.Default);
            OwnerQueryable.ExceptByInterpolated(CatQueryable, default(Cat)!, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.ExceptByInterpolated(CatQueryable, default(Cat)!, (x, o) => x.SpliceConstant(x.Data), EqualityComparer<Cat>.Default);
        });
    }

    [Fact]
    public void FirstInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.FirstInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.FirstInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void FirstOrDefaultInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.FirstOrDefaultInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.FirstOrDefaultInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void GroupByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.GroupByInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.GroupByInterpolated((x, o) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.GroupByInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.GroupByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
            OwnerQueryable.GroupByInterpolated(o => 42, (x, o) => x.SpliceConstant(42));
            OwnerQueryable.GroupByInterpolated(o => 42, (x, o) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.GroupByInterpolated((x, o) => x.SpliceConstant(42), o => o);
            OwnerQueryable.GroupByInterpolated((x, o) => x.SpliceConstant(42), o => o, EqualityComparer<int>.Default);
            OwnerQueryable.GroupByInterpolated((x, o) => x.SpliceConstant(42), (x, o) => x.SpliceConstant(42));
            OwnerQueryable.GroupByInterpolated((x, o) => x.SpliceConstant(42), (x, o) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.GroupByInterpolated(42, o => 42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.GroupByInterpolated(42, o => 42, (x, o) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
            OwnerQueryable.GroupByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), o => 42);
            OwnerQueryable.GroupByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), o => 42, EqualityComparer<int>.Default);
            OwnerQueryable.GroupByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.GroupByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), (x, o) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
        });
    }

    [Fact]
    public void GroupJoinInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), (x, c) => x.SpliceConstant(42), (x, o, c) => x.SpliceConstant(42));
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), (x, c) => x.SpliceConstant(42), (x, o, c) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), (x, c) => x.SpliceConstant(x.Data), (x, o, c) => x.SpliceConstant(x.Data));
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), (x, c) => x.SpliceConstant(x.Data), (x, o, c) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), c => 42, (o, c) => 42);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), c => 42, (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), c => 42, (o, c) => 42);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), c => 42, (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, o => 42, (x, c) => x.SpliceConstant(42), (o, c) => 42);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, o => 42, (x, c) => x.SpliceConstant(42), (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, o => 42, (x, c) => x.SpliceConstant(x.Data), (o, c) => 42);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, o => 42, (x, c) => x.SpliceConstant(x.Data), (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, o => 42, c => 42, (x, o, c) => x.SpliceConstant(42));
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, o => 42, c => 42, (x, o, c) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, o => 42, c => 42, (x, o, c) => x.SpliceConstant(x.Data));
            OwnerQueryable.GroupJoinInterpolated(CatQueryable, 42, o => 42, c => 42, (x, o, c) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
        });
    }

    [Fact]
    public void IntersectByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.IntersectByInterpolated(CatQueryable, (x, o) => x.SpliceConstant(default(Cat)!));
            OwnerQueryable.IntersectByInterpolated(CatQueryable, (x, o) => x.SpliceConstant(default(Cat)!), EqualityComparer<Cat>.Default);
            OwnerQueryable.IntersectByInterpolated(CatQueryable, default(Cat)!, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.IntersectByInterpolated(CatQueryable, default(Cat)!, (x, o) => x.SpliceConstant(x.Data), EqualityComparer<Cat>.Default);
        });
    }

    [Fact]
    public void JoinInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.JoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), (x, c) => x.SpliceConstant(42), (x, o, c) => x.SpliceConstant(42));
            OwnerQueryable.JoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), (x, c) => x.SpliceConstant(42), (x, o, c) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), (x, c) => x.SpliceConstant(x.Data), (x, o, c) => x.SpliceConstant(x.Data));
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), (x, c) => x.SpliceConstant(x.Data), (x, o, c) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
            OwnerQueryable.JoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), c => 42, (o, c) => 42);
            OwnerQueryable.JoinInterpolated(CatQueryable, (x, o) => x.SpliceConstant(42), c => 42, (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), c => 42, (o, c) => 42);
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, (x, o) => x.SpliceConstant(x.Data), c => 42, (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.JoinInterpolated(CatQueryable, o => 42, (x, c) => x.SpliceConstant(42), (o, c) => 42);
            OwnerQueryable.JoinInterpolated(CatQueryable, o => 42, (x, c) => x.SpliceConstant(42), (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, o => 42, (x, c) => x.SpliceConstant(x.Data), (o, c) => 42);
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, o => 42, (x, c) => x.SpliceConstant(x.Data), (o, c) => 42, EqualityComparer<int>.Default);
            OwnerQueryable.JoinInterpolated(CatQueryable, o => 42, c => 42, (x, o, c) => x.SpliceConstant(42));
            OwnerQueryable.JoinInterpolated(CatQueryable, o => 42, c => 42, (x, o, c) => x.SpliceConstant(42), EqualityComparer<int>.Default);
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, o => 42, c => 42, (x, o, c) => x.SpliceConstant(x.Data));
            OwnerQueryable.JoinInterpolated(CatQueryable, 42, o => 42, c => 42, (x, o, c) => x.SpliceConstant(x.Data), EqualityComparer<int>.Default);
        });
    }

    [Fact]
    public void LastInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.LastInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.LastInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void LastOrDefaultInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.LastOrDefaultInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.LastOrDefaultInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void MaxInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.MaxInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.MaxInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void MaxByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.MaxByInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.MaxByInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            // N.B. these appear to be framework bugs, as it should be a comparer for TKey
            OwnerQueryable.MaxByInterpolated((x, o) => x.SpliceConstant(42), Comparer<Owner>.Default);
            OwnerQueryable.MaxByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), Comparer<Owner>.Default);
        });
    }

    [Fact]
    public void MinInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.MinInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.MinInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void MinByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.MinByInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.MinByInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            // N.B. these appear to be framework bugs, as it should be a comparer for TKey
            OwnerQueryable.MinByInterpolated((x, o) => x.SpliceConstant(42), Comparer<Owner>.Default);
            OwnerQueryable.MinByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), Comparer<Owner>.Default);
        });
    }

    [Fact]
    public void OrderByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.OrderByInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.OrderByInterpolated((x, o) => x.SpliceConstant(42), Comparer<int>.Default);
            OwnerQueryable.OrderByInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.OrderByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), Comparer<int>.Default);
        });
    }

    [Fact]
    public void OrderByDescendingInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.OrderByDescendingInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.OrderByDescendingInterpolated((x, o) => x.SpliceConstant(42), Comparer<int>.Default);
            OwnerQueryable.OrderByDescendingInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.OrderByDescendingInterpolated(42, (x, o) => x.SpliceConstant(x.Data), Comparer<int>.Default);
        });
    }

    [Fact]
    public void SelectInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.SelectInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.SelectInterpolated("foo", (x, o) => x.SpliceConstant(42));
        });
    }

    [Fact]
    public void SelectManyInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.SelectManyInterpolated((x, o) => x.SpliceConstant("foo"));
            OwnerQueryable.SelectManyInterpolated("foo", (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.SelectManyInterpolated(o => o.Dogs, (x, o, d) => x.SpliceConstant(42));
            OwnerQueryable.SelectManyInterpolated((x, o) => x.SpliceConstant("foo"), (x, o, d) => x.SpliceConstant(42));
            OwnerQueryable.SelectManyInterpolated((x, o) => x.SpliceConstant("foo"), (o, d) => d);
            OwnerQueryable.SelectManyInterpolated("foo", (x, o) => x.SpliceConstant("foo"), (x, o, d) => x.SpliceConstant(42));
            OwnerQueryable.SelectManyInterpolated("foo", (x, o) => x.SpliceConstant("foo"), (o, d) => d);
            OwnerQueryable.SelectManyInterpolated("foo", o => o.Dogs, (x, o, d) => x.SpliceConstant(42));
        });
    }

    [Fact]
    public void SingleInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.SingleInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.SingleInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void SingleOrDefaultInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.SingleOrDefaultInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.SingleOrDefaultInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void SkipWhileInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.SkipWhileInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.SkipWhileInterpolated(false, (x, o) => x.SpliceConstant(false));
        });
    }

    [Fact]
    public void SumInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.SumInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.SumInterpolated((x, o) => x.SpliceConstant(42d));
            OwnerQueryable.SumInterpolated((x, o) => x.SpliceConstant(42L));
            OwnerQueryable.SumInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.SumInterpolated(42d, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.SumInterpolated(42L, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void TakeWhileInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.TakeWhileInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.TakeWhileInterpolated(false, (x, o) => x.SpliceConstant(false));
        });
    }

    [Fact]
    public void ThenByInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.Order().ThenByInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.Order().ThenByInterpolated((x, o) => x.SpliceConstant(42), Comparer<int>.Default);
            OwnerQueryable.Order().ThenByInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.Order().ThenByInterpolated(42, (x, o) => x.SpliceConstant(x.Data), Comparer<int>.Default);
        });
    }

    [Fact]
    public void ThenByDescendingInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.Order().ThenByDescendingInterpolated((x, o) => x.SpliceConstant(42));
            OwnerQueryable.Order().ThenByDescendingInterpolated((x, o) => x.SpliceConstant(42), Comparer<int>.Default);
            OwnerQueryable.Order().ThenByDescendingInterpolated(42, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.Order().ThenByDescendingInterpolated(42, (x, o) => x.SpliceConstant(x.Data), Comparer<int>.Default);
        });
    }

    [Fact]
    public void UnionByInterpolated_should_resolve() {
        // I have no idea why the API for this method differs significantly from IntersectBy, but whatever
        Resolves(() => {
            OwnerQueryable.UnionByInterpolated(OwnerQueryable, (x, o) => x.SpliceConstant(default(Cat)!));
            OwnerQueryable.UnionByInterpolated(OwnerQueryable, (x, o) => x.SpliceConstant(default(Cat)!), EqualityComparer<Cat>.Default);
            OwnerQueryable.UnionByInterpolated(OwnerQueryable, default(Cat)!, (x, o) => x.SpliceConstant(x.Data));
            OwnerQueryable.UnionByInterpolated(OwnerQueryable, default(Cat)!, (x, o) => x.SpliceConstant(x.Data), EqualityComparer<Cat>.Default);
        });
    }

    [Fact]
    public void WhereInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.WhereInterpolated((x, o) => x.SpliceConstant(true));
            OwnerQueryable.WhereInterpolated(false, (x, o) => x.SpliceConstant(x.Data));
        });
    }

    [Fact]
    public void ZipInterpolated_should_resolve() {
        Resolves(() => {
            OwnerQueryable.ZipInterpolated(CatQueryable, (x, o, c) => x.SpliceConstant(42));
            OwnerQueryable.ZipInterpolated(CatQueryable, 42, (x, o, c) => x.SpliceConstant(x.Data));
        });
    }
}
