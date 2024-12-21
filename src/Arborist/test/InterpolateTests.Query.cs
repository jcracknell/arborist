using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_select_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate(
            (x, o) => from c in o.Cats select c.Name
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.Select(c => c.Name)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_from_clause_with_cast() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate(
            (x, o) => from object c in o.Cats select c.GetHashCode()
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.Cast<object>().Select(c => c.GetHashCode())
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_join_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate(new Owner(), (x, o) =>
            from c in o.Cats
            join c1 in o.Cats on c.Id equals c1.Id
            select c1.Name
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.Join(
                o.Cats,
                c => c.Id,
                c1 => c1.Id,
                (c, c1) => c1.Name
            )
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_join_clause_with_cast() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from c in o.Cats
            join object c1 in o.Cats on c.Id equals c1.GetHashCode()
            select c1.GetHashCode()
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.Join(
                o.Cats.Cast<object>(),
                c => c.Id,
                c1 => c1.GetHashCode(),
                (c, c1) => c1.GetHashCode()
            )
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_join_into_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from c in o.Cats
            join c1 in o.Cats on c.Id equals c1.Id into cs
            from cc in cs
            select cc.Age
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.GroupJoin(
                o.Cats,
                c => c.Id,
                c1 => c1.Id,
                (c, cs) => new { c, cs }
            )
            .SelectMany(
                __v0 => __v0.cs,
                (__v0, cc) => cc.Age
            )
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_let_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from c in o.Cats
            let n = o.Name
            select c.Name + n
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.Select(c => new { c, n = o.Name })
            .Select(__v0 => __v0.c.Name + __v0.n)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_orderby_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from c in o.Cats
            orderby c.Name ascending, c.Age descending
            select c.Name
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.OrderBy(c => c.Name)
            .ThenByDescending(c => c.Age)
            .Select(c => c.Name)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_where_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from c in o.Cats
            where c.Age == 8
            select c.Name
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.Where(c => c.Age == 8)
            .Select(c => c.Name)
        );

        Assert.Equivalent(expected, interpolated);
    }
}
