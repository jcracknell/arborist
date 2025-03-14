using Arborist.TestFixtures;

namespace Arborist;

public class ExpressionEqualityComparerTests {
    public ExpressionEqualityComparerTests() { }

    private ExpressionEqualityComparerTests(string arg) { }
    private int this[int index] => throw new NotImplementedException();
    private int this[int i0, int i2] => throw new NotImplementedException();
    private int InstanceProperty { get; init; } = default!;
    private static int StaticProperty => throw new NotImplementedException();
    private int InstanceMethod(string arg0) => throw new NotImplementedException();
    private static int StaticMethod(string arg0) => throw new NotImplementedException();

    private static void Equivalent(Expression? a, Expression? b) {
        if(a is not null && b is not null)
            Assert.Equal(
                ExpressionEqualityComparer.Default.GetHashCode(a),
                ExpressionEqualityComparer.Default.GetHashCode(b)
            );

        Assert.Equal(a, b, ExpressionEqualityComparer.Default);
    }

    private static void NotEquivalent(Expression? a, Expression? b) {
        if(a is not null && b is not null)
            Assert.NotEqual(
                ExpressionEqualityComparer.Default.GetHashCode(a),
                ExpressionEqualityComparer.Default.GetHashCode(b)
            );

        Assert.NotEqual(a, b, ExpressionEqualityComparer.Default);
    }

    [Fact]
    public void Non_static_lambdas_should_be_separate_instances() {
        Assert.NotEqual(
            ExpressionOnNone.Of(() => 1),
            ExpressionOnNone.Of(() => 1)
        );
    }

    [Fact]
    public void GetHashCode_should_throw_ArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => {
            _ = ExpressionEqualityComparer.Default.GetHashCode(null!);
        });
    }

    [Fact]
    public void Should_work_as_expected_for_nulls() {
        Equivalent(null, null);
        NotEquivalent(Expression.Constant(1), null);
        NotEquivalent(null, Expression.Constant(1));
    }

    [Fact]
    public void Should_work_as_expected_for_BinaryExpression() {
        Equivalent(
            Expression.Equal(Expression.Constant(1), Expression.Constant(2)),
            Expression.Equal(Expression.Constant(1), Expression.Constant(2))
        );
        NotEquivalent(
            Expression.Equal(Expression.Constant(1), Expression.Constant(2)),
            Expression.Equal(Expression.Constant(2), Expression.Constant(2))
        );
        NotEquivalent(
            Expression.Equal(Expression.Constant(1), Expression.Constant(2)),
            Expression.Equal(Expression.Constant(1), Expression.Constant(1))
        );
        NotEquivalent(
            Expression.Equal(Expression.Constant(1), Expression.Constant(2)),
            Expression.NotEqual(Expression.Constant(1), Expression.Constant(2))
        );
    }

    [Fact]
    public void Should_work_as_expected_for_ConditionalExpression() {
        Equivalent(
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("b"), typeof(string)),
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("b"), typeof(string))
        );
        NotEquivalent(
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("b"), typeof(string)),
            Expression.Condition(Expression.Constant(false), Expression.Constant("a"), Expression.Constant("b"), typeof(string))
        );
        NotEquivalent(
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("b"), typeof(string)),
            Expression.Condition(Expression.Constant(true), Expression.Constant("b"), Expression.Constant("b"), typeof(string))
        );
        NotEquivalent(
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("b"), typeof(string)),
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("a"), typeof(string))
        );
        NotEquivalent(
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("b"), typeof(string)),
            Expression.Condition(Expression.Constant(true), Expression.Constant("a"), Expression.Constant("b"), typeof(object))
        );
    }

    [Fact]
    public void Should_work_as_expected_for_ConstantExpression() {
        Equivalent(
            Expression.Constant("a", typeof(string)),
            Expression.Constant("a", typeof(string))
        );
        NotEquivalent(
            Expression.Constant("a", typeof(string)),
            Expression.Constant("b", typeof(string))
        );
        NotEquivalent(
            Expression.Constant("a", typeof(string)),
            Expression.Constant("a", typeof(object))
        );
        NotEquivalent(
            Expression.Constant(new object(), typeof(object)),
            Expression.Constant(new object(), typeof(object))
        );
    }

    [Fact]
    public void Should_work_as_expected_for_DefaultExpression() {
        Equivalent(
            Expression.Default(typeof(string)),
            Expression.Default(typeof(string))
        );
        NotEquivalent(
            Expression.Default(typeof(string)),
            Expression.Default(typeof(object))
        );
    }

    [Fact]
    public void Should_work_as_expected_for_IndexExpression() {
        Equivalent(
            ExpressionOnNone.Of(() => this[1]).Body,
            ExpressionOnNone.Of(() => this[1]).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => this[1]).Body,
            ExpressionOnNone.Of(() => this[2]).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => this[1]).Body,
            ExpressionOnNone.Of(() => this[1, 1]).Body
        );
    }

    [Fact]
    public void Should_work_as_expected_for_LambdaExpression() {
        Equivalent(
            ExpressionOnNone.Of(() => 1),
            ExpressionOnNone.Of(() => 1)
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => 1),
            ExpressionOnNone.Of(() => 2)
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => 1),
            ExpressionOn<Cat>.Of(c => 1)
        );
        NotEquivalent(
            (Expression<Func<string>>)(() => "foo"),
            (Expression<Func<object>>)(() => "foo")
        );
    }

    [Fact]
    public void Should_work_as_expected_for_ListInitExpression() {
        Equivalent(
            ExpressionOnNone.Of(() => new List<string> { "foo" }).Body,
            ExpressionOnNone.Of(() => new List<string> { "foo" }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new List<string> { "foo" }).Body,
            ExpressionOnNone.Of(() => new List<string> { "bar" }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new List<string> { "foo" }).Body,
            ExpressionOnNone.Of(() => new List<string> { "foo", "foo" }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new List<string> { "foo" }).Body,
            ExpressionOnNone.Of(() => new HashSet<string> { "foo" }).Body
        );
    }

    [Fact]
    public void Should_work_as_expected_for_MemberExpression() {
        Equivalent(
            ExpressionOnNone.Of(() => InstanceProperty).Body,
            ExpressionOnNone.Of(() => InstanceProperty).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => InstanceProperty).Body,
            ExpressionOnNone.Of(() => StaticProperty).Body
        );
    }

    [Fact]
    public void Should_work_as_expected_for_MemberInitExpression() {
        Equivalent(
            ExpressionOnNone.Of(() => new Cat { Name = "Garfield" }).Body,
            ExpressionOnNone.Of(() => new Cat { Name = "Garfield" }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new Cat { Name = "Garfield" }).Body,
            ExpressionOnNone.Of(() => new Cat { Name = "Nermal" }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new ExpressionEqualityComparerTests() { InstanceProperty = 1 }).Body,
            ExpressionOnNone.Of(() => new ExpressionEqualityComparerTests("foo") { InstanceProperty = 1 }).Body
        );
        Equivalent(
            ExpressionOnNone.Of(() => new Cat { Owner = new Owner() }).Body,
            ExpressionOnNone.Of(() => new Cat { Owner = new Owner() }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new Cat { Owner = new Owner() }).Body,
            ExpressionOnNone.Of(() => new Cat { Owner = new Owner { Name = "Jon" } }).Body
        );
        Equivalent(
            ExpressionOnNone.Of(() => new NestedCollectionInitializerFixture<string> { List = { "foo" } }).Body,
            ExpressionOnNone.Of(() => new NestedCollectionInitializerFixture<string> { List = { "foo" } }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new NestedCollectionInitializerFixture<string> { List = { "foo" } }).Body,
            ExpressionOnNone.Of(() => new NestedCollectionInitializerFixture<string> { List = { "bar" } }).Body
        );
    }

    [Fact]
    public void Should_work_as_expected_for_MethodCallExpression() {
        Equivalent(
            ExpressionOnNone.Of(() => InstanceMethod("foo")).Body,
            ExpressionOnNone.Of(() => InstanceMethod("foo")).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => InstanceMethod("foo")).Body,
            ExpressionOnNone.Of(() => InstanceMethod("bar")).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => InstanceMethod("foo")).Body,
            ExpressionOnNone.Of(() => StaticMethod("foo")).Body
        );
    }

    [Fact]
    public void Should_work_as_expected_for_NewArrayExpression() {
        Equivalent(
            ExpressionOnNone.Of(() => new string[1]).Body,
            ExpressionOnNone.Of(() => new string[1]).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new string[1]).Body,
            ExpressionOnNone.Of(() => new string[2]).Body
        );
        Equivalent(
            ExpressionOnNone.Of(() => new[] { "foo" }).Body,
            ExpressionOnNone.Of(() => new[] { "foo" }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new[] { "foo" }).Body,
            ExpressionOnNone.Of(() => new[] { "foo", "bar" }).Body
        );
        NotEquivalent(
            ExpressionOnNone.Of(() => new[] { "foo" }).Body,
            ExpressionOnNone.Of(() => new string[1]).Body
        );
    }

    [Fact]
    public void Should_work_as_expected_for_ParameterExpression() {
        Equivalent(
            Expression.Parameter(typeof(string), "a"),
            Expression.Parameter(typeof(string), "a")
        );
        NotEquivalent(
            Expression.Parameter(typeof(string), "a"),
            Expression.Parameter(typeof(string), "b")
        );
        NotEquivalent(
            Expression.Parameter(typeof(string), "a"),
            Expression.Parameter(typeof(object), "a")
        );
        NotEquivalent(
            Expression.Parameter(typeof(string), "a"),
            Expression.Parameter(typeof(string))
        );
    }

    [Fact]
    public void Should_work_as_expected_for_TypeBinaryExpression() {
        Equivalent(
            ExpressionOn<object>.Of(o => o is string).Body,
            ExpressionOn<object>.Of(o => o is string).Body
        );
        NotEquivalent(
            ExpressionOn<object>.Of(o => o is string).Body,
            ExpressionOn<object>.Of(o => o is int).Body
        );
        NotEquivalent(
            ExpressionOn<object>.Of(o => o is string).Body,
            ExpressionOn<Cat>.Of(c => (object)c.Name is string).Body
        );
    }

    [Fact]
    public void Should_work_as_expected_for_UnaryExpression() {
        Equivalent(
            Expression.Convert(Expression.Constant(1), typeof(long)),
            Expression.Convert(Expression.Constant(1), typeof(long))
        );
        NotEquivalent(
            Expression.Convert(Expression.Constant(1), typeof(long)),
            Expression.Convert(Expression.Constant(2), typeof(long))
        );
        NotEquivalent(
            Expression.Convert(Expression.Constant(1), typeof(long)),
            Expression.Convert(Expression.Constant(1), typeof(int))
        );
        NotEquivalent(
            Expression.UnaryPlus(Expression.Constant(1)),
            Expression.Negate(Expression.Constant(1))
        );
    }
}
