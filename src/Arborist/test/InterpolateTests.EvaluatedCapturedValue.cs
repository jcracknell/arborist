using Arborist.TestFixtures;

namespace Arborist;

public partial class InterpolateTests {
    [Fact]
    public void Should_handle_captured_local() {
        var value = "foo";

        var expected = ExpressionOnNone.Of(() => "foo");
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(value));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_anonymous_class_instance() {
        var value = new { Foo = "foo" };

        var expected = ExpressionOnNone.Of(() => "foo");
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(value.Foo));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_binary_expression() {
        var value = "foo";

        var expected = ExpressionOnNone.Of(() => "foobar");
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(value + "bar"));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_unary_expression() {
        var value = 42;

        var expected = ExpressionOnNone.Of(() => -42);
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(-value));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_methodcallexpression() {
        var value = "foo";

        var expected = ExpressionOnNone.Of(() => "barfoo");
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(string.Concat("bar", value)));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_new_expression() {
        var c = '0';
        var count = 3;

        var expected = ExpressionOnNone.Of(() => "000");
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(new string(c, count)));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_object_initializer() {
        var value = "Garfield";

        var expected = Expression.Lambda<Func<Cat>>(Expression.Constant(new Cat { Name = value }));
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(new Cat { Name = value }));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_list_initializer() {
        var count = 1;
        var value = "Garfield";

        var expected = Expression.Lambda<Func<List<string>>>(Expression.Constant(new List<string>(count) { value }));
        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(new List<string>(count) { value }));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_lambda() {
        var value = "foo";

        var expected = ExpressionOnNone.Constant(
            Array.Empty<Cat>().Where(c => c.Name == value)
        );

        var interpolated = ExpressionOnNone.Interpolate(x => x.SpliceConstant(
            Array.Empty<Cat>().Where(c => c.Name == value)
        ));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_quoted_lambda() {
        var value = "foo";

        var expected = ExpressionOnNone.Constant(
            Array.Empty<Cat>().AsQueryable().Where(c => c.Name == value)
        );

        var interpolated = ExpressionOnNone.Interpolate(x => x.SpliceConstant(
            Array.Empty<Cat>().AsQueryable().Where(c => c.Name == value)
        ));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_nested_quoted_lambda() {
        // This test case is important as it validates that expression binding correctly tracks
        // quoting UnaryExpression instances
        var value = "foo";

        var expected = ExpressionOnNone.Constant(
            Array.Empty<Cat>().AsQueryable().Where(
                c => Array.Empty<Cat>().AsQueryable().Any(c0 => c0.Name == value)
            )
        );

        var interpolated = ExpressionOnNone.Interpolate(x => x.SpliceConstant(
            Array.Empty<Cat>().AsQueryable().Where(
                c => Array.Empty<Cat>().AsQueryable().Any(c0 => c0.Name == value)
            )
        ));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_from_clause() {
        var cats = new[] { new Cat() };

        var expected = ExpressionOnNone.Constant(
            from c in cats
            select c.Name
        );

        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(
            from c in cats
            select c.Name
        ));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_group_by_clause() {
        var i = 42;
        var s = "foo";

        var expected = ExpressionOnNone.Constant(
            from o in new[] { new Owner() }
            group o.Id + i by o.Name + s
        );

        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(
            from o in new[] { new Owner() }
            group o.Id + i by o.Name + s
        ));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_join_clause() {
        var owners = new[] { new Owner() };
        var value = 42;

        var expected = ExpressionOnNone.Constant(
            from o in owners
            join o1 in owners on o.Id + value equals value + o1.Id
            select o.Id + value
        );

        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(
            from o in owners
            join o1 in owners on o.Id + value equals value + o1.Id
            select o.Id + value
        ));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_let_clause() {
        var value = "foo";

        var expected = ExpressionOnNone.Constant(
            from o in new[] { new Owner() }
            let n = o.Name + value
            select n
        );

        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(
            from o in new[] { new Owner() }
            let n = o.Name + value
            select n
        ));

        Assert.Equivalent(expected, interpolated);
    }

    [Fact]
    public void Should_handle_captured_local_in_orderby_clause() {
        var s = "foo";
        var i = 42;

        var expected = ExpressionOnNone.Constant(
            from o in new[] { new Owner() }
            orderby o.Name + s, o.Id + i
            select o
        );

        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(
            from o in new[] { new Owner() }
            orderby o.Name + s, o.Id + i
            select o
        ));

        Assert.Equivalent(expected, interpolated);
    }


    [Fact]
    public void Should_handle_captured_local_in_where_clause() {
        var value = "foo";

        var expected = ExpressionOnNone.Constant(
            from o in new[] { new Owner() }
            where o.Name == value
            select o
        );

        var interpolated = InterpolationTestOnNone.Interpolate(x => x.SpliceConstant(
            from o in new[] { new Owner() }
            where o.Name == value
            select o
        ));

        Assert.Equivalent(expected, interpolated);
    }
}
