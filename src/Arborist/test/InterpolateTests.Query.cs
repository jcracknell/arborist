using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_select_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatName = ExpressionOn<Cat>.Of(c => c.Name)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from c in x.SpliceBody(o, x.Data.OwnerCats)
            select x.SpliceBody(c, x.Data.CatName)
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            select c.Name
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_from_clause_with_cast() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            ObjectHashCode = ExpressionOn<object>.Of(o => o.GetHashCode())
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from object c in x.SpliceBody(o, x.Data.OwnerCats)
            select x.SpliceBody(c, x.Data.ObjectHashCode)
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from object c in o.Cats
            select c.GetHashCode()
        );

        Assert.Equivalent(expected, interpolated);
    }
    
    [Fact]
    public void Should_handle_from_clause_with_cast_in_subsequent_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            ObjectHashCode = ExpressionOn<object>.Of(o => o.GetHashCode())
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from a in o.Cats
            from object c in x.SpliceBody(o, x.Data.OwnerCats)
            select x.SpliceBody(c, x.Data.ObjectHashCode)
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from a in o.Cats
            from object c in o.Cats
            select c.GetHashCode()
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_join_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatId = ExpressionOn<Cat>.Of(c => c.Id)
        };
    
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from c in o.Cats
            join c1 in x.SpliceBody(o, x.Data.OwnerCats) on x.SpliceBody(c, x.Data.CatId) equals x.SpliceValue(42)
            select c1.Name
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            join c1 in o.Cats on c.Id equals 42
            select c1.Name
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_join_clause_with_cast() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatId = ExpressionOn<Cat>.Of(c => c.Id)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from c in o.Cats
            join object c1 in x.SpliceBody(o, x.Data.OwnerCats) on x.SpliceBody(c, x.Data.CatId) equals x.SpliceValue(42)
            select c1.GetHashCode()
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            join object c1 in o.Cats on c.Id equals 42
            select c1.GetHashCode()
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_join_into_clause() {
        var interpolated = ExpressionOn<Owner>.Interpolate(ExpressionOn<Owner>.Of(o => o.Cats), (x, o) =>
            from c in x.SpliceBody(o, x.Data)
            join c1 in x.SpliceBody(o, x.Data) on c.Id equals c1.Id into cs
            from cc in cs
            select cc.Age
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            join c1 in o.Cats on c.Id equals c1.Id into cs
            from cc in cs
            select cc.Age
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_let_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats)        ,
            CatName = ExpressionOn<Cat>.Of(c => c.Name)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from c in x.SpliceBody(o, x.Data.OwnerCats)
            let n = x.SpliceBody(c, x.Data.CatName)
            select c.Name + n
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            let n = c.Name
            select c.Name + n
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_orderby_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatAge = ExpressionOn<Cat>.Of(c => c.Age)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from c in x.SpliceBody(o, x.Data.OwnerCats)
            orderby x.SpliceBody(c, ExpressionOn<Cat>.Identity).Name ascending, x.SpliceBody(c, x.Data.CatAge) descending
            select c.Name
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            orderby c.Name ascending, c.Age descending
            select c.Name
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_where_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatAge = ExpressionOn<Cat>.Of(c => c.Age)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from c in x.SpliceBody(o, x.Data.OwnerCats)
            where x.SpliceBody(c, x.Data.CatAge) == 8
            select c.Name
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            where c.Age == 8
            select c.Name
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_from_clause() {
        var data = ExpressionOn<Owner>.Of(o => o.Cats);
    
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from a in x.SpliceBody(o, x.Data)
            from b in o.Cats
            from c in o.Cats
            from d in o.Cats
            select x.SpliceBody(b, ExpressionOn<Cat>.Identity)
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            from d in o.Cats
            select b
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_groupby_clause() {
        var data = new {
            CatId = ExpressionOn<Cat>.Of(c => c.Id),
            CatAge = ExpressionOn<Cat>.Of(c => c.Age)
        };
    
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            group x.SpliceBody(a, x.Data.CatId) by x.SpliceBody(a, x.Data.CatAge)
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            group a.Id by a.Age
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_let_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            OwnerName = ExpressionOn<Owner>.Of(o => o.Name),
            OwnerId = ExpressionOn<Owner>.Of(o => o.Id)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from c in x.SpliceBody(o, x.Data.OwnerCats)
            let n = x.SpliceBody(o, x.Data.OwnerName)
            let m = x.SpliceBody(o, x.Data.OwnerId)
            select c.Name + n + m + x.SpliceValue("foo")
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from c in o.Cats
            let n = o.Name
            let m = o.Id
            select c.Name + n + m + "foo"
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_join_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatId = ExpressionOn<Cat>.Of(c => c.Id)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from a in x.SpliceBody(o, x.Data.OwnerCats)
            join b in o.Cats on a.Id equals b.Id
            join c in o.Cats on a.Id equals x.SpliceValue(42)
            join d in x.SpliceBody(o, x.Data.OwnerCats) on x.SpliceBody(c, x.Data.CatId) equals x.SpliceBody(d, x.Data.CatId)
            select a
        );


        var expected = ExpressionOn<Owner>.Of(o =>
            from a in o.Cats
            join b in o.Cats on a.Id equals b.Id
            join c in o.Cats on a.Id equals 42
            join d in o.Cats on c.Id equals d.Id
            select a
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_orderby_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatName = ExpressionOn<Cat>.Of(c => c.Name)
        };
        
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from a in x.SpliceBody(o, x.Data.OwnerCats)
            from b in o.Cats
            from c in o.Cats
            orderby x.SpliceBody(a, x.Data.CatName), x.SpliceValue(42)
            select a
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            orderby a.Name, 42
            select a
        );

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_where_clause() {
        var data = new {
            OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
            CatId = ExpressionOn<Cat>.Of(c => c.Id)
        };
    
        var interpolated = ExpressionOn<Owner>.Interpolate(data, (x, o) =>
            from a in x.SpliceBody(o, x.Data.OwnerCats)
            from b in o.Cats
            from c in o.Cats
            where x.SpliceBody(a, x.Data.CatId) % 2 == x.SpliceValue(0)
            select a
        );

        var expected = ExpressionOn<Owner>.Of(o =>
            from a in o.Cats
            from b in o.Cats
            from c in o.Cats
            where a.Id % 2 == 0
            select a
        );

        Assert.Equivalent(expected, interpolated);
    }
}
