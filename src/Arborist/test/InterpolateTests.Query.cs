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

    [Fact]
    public void Should_handle_transparent_identifier_in_from_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            from d in o.Cats
            select b
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.SelectMany(a => o.Cats, (a, b) => new { a, b })
            .SelectMany(__v0 => o.Cats, (__v0, c) => new { __v0, c })
            .SelectMany(__v1 => o.Cats, (__v1, d) => __v1.__v0.b)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_groupby_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            group a by a.Age
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(o =>
            o.Cats.SelectMany(a => o.Cats, (a, b) => new { a, b })
            .SelectMany(__v0 => o.Cats, (__v0, c) => new { __v0, c })
            .GroupBy(__v1 => __v1.__v0.a.Age, __v1 => __v1.__v0.a)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_let_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from c in o.Cats
            let n = o.Name
            let m = o.Id
            select c.Name + n + m
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(
            o => o.Cats.Select(c => new { c, n = o.Name })
            .Select(__v0 => new { __v0, m = o.Id })
            .Select(__v1 => __v1.__v0.c.Name + __v1.__v0.n + __v1.m)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_join_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from a in o.Cats
            join b in o.Cats on a.Id equals b.Id
            join c in o.Cats on a.Id equals c.Id
            join d in o.Cats on a.Id equals d.Id
            select a
        );
        #pragma warning restore


        var expected = ExpressionOn<Owner>.Of(o =>
            o.Cats.Join(o.Cats, a => a.Id, b => b.Id, (a, b) => new { a, b })
            .Join(o.Cats, __v0 => __v0.a.Id, c => c.Id, (__v0, c) => new { __v0, c })
            .Join(o.Cats, __v1 => __v1.__v0.a.Id, d => d.Id, (__v1, d) => __v1.__v0.a)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_orderby_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            orderby a.Name
            select a
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(o =>
            o.Cats.SelectMany(a => o.Cats, (a, b) => new { a, b })
            .SelectMany(__v0 => o.Cats, (__v0, c) => new { __v0, c })
            .OrderBy(__v1 => __v1.__v0.a.Name)
            .Select(__v1 => __v1.__v0.a)
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_where_clause() {
        #pragma warning disable ARB003
        var interpolated = ExpressionOn<Owner>.Interpolate((x, o) =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            where a.Id % 2 == 0
            select a
        );
        #pragma warning restore

        var expected = ExpressionOn<Owner>.Of(o =>
            o.Cats.SelectMany(a => o.Cats, (a, b) => new { a, b })
            .SelectMany(__v0 => o.Cats, (__v0, c) => new { __v0, c })
            .Where(__v1 => __v1.__v0.a.Id % 2 == 0)
            .Select(__v1 => __v1.__v0.a)
        );

        Assert.Equivalent(expected, interpolated);
    }
}
