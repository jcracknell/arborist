using Arborist.TestFixtures;

namespace Arborist.Interpolation.Internal;

public class ReflectivePartialSplicedExpressionEvaluatorTests {
    private static string StaticProperty { get; } = "foo";
    private static readonly string StaticField = "foo";
    private static string StaticMethod(string argument) => argument;
    private string InstanceMethod(string argument) => argument;

    private class InitializerFixture {
        public List<string> NestedCollection { get; init; } = new();
        public Owner NestedObject { get; init; } = new();
    }

    [Fact]
    public void Should_evaluate_InterpolationContext_data_access() {
        var access = Expression.Property(
            Expression.Parameter(typeof(IInterpolationContext<int>)),
            nameof(IInterpolationContext<int>.Data)
        );

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(42, access, out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Should_evaluate_constant() {
        var constant = Expression.Constant(42);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), constant, out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Should_evaluate_instance_field() {
        var instance = new MemberFixture { InstanceField = "foo" };
        var expr = ExpressionOnNone.Of(() => instance.InstanceField);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equal(instance.InstanceField, value);
    }

    [Fact]
    public void Should_evaluate_static_field() {
        var expr = ExpressionOnNone.Of(() => StaticField);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equal(StaticField, value);
    }

    [Fact]
    public void Should_evaluate_instance_property() {
        var instance = new MemberFixture { InstanceProperty = "foo" };
        var expr = ExpressionOnNone.Of(() => instance.InstanceProperty);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equal(instance.InstanceProperty, value);
    }

    [Fact]
    public void Should_evaluate_static_property() {
        var expr = ExpressionOnNone.Of(() => StaticProperty);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equal(StaticProperty, value);
    }

    [Fact]
    public void Should_evaluate_instance_method_call() {
        var expr = ExpressionOnNone.Of(() => InstanceMethod("foo"));

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equal(InstanceMethod("foo"), value);
    }

    [Fact]
    public void Should_evaluate_static_method_call() {
        var expr = ExpressionOnNone.Of(() => StaticMethod("foo"));

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equal(StaticMethod("foo"), value);
    }

    [Fact]
    public void Should_evaluate_quoted_lambda_expression() {
        var lambda = ExpressionOn<Cat>.Of(c => c.Name);
        var quoted = Expression.Quote(lambda);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), quoted, out var value));
        Assert.Equal(lambda, value);
    }

    [Fact]
    public void Should_evaluate_boxing_conversion() {
        var convert = Expression.Convert(Expression.Constant(42), typeof(object));

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), convert, out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Should_evaluate_supertype_conversion() {
        var convert = Expression.Convert(Expression.Constant("foo"), typeof(object));

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), convert, out var value));
        Assert.Equal("foo", value);
    }

    [Fact]
    public void Should_evaluate_Nullable_conversion() {
        var convert = Expression.Convert(Expression.Constant(42), typeof(Nullable<int>));

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), convert, out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Should_evaluate_new() {
        var newExpr = Expression.New(
            typeof(string).GetConstructor(new[] { typeof(char), typeof(int) })!,
            Expression.Constant('a'),
            Expression.Constant(3)
        );

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), newExpr, out var value));
        Assert.Equal("aaa", value);
    }

    [Fact]
    public void Should_evaluate_collection_initializer() {
        var expr = ExpressionOnNone.Of(() => new List<string> { "foo" });

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equivalent(new List<string> { "foo" }, value);
    }

    [Fact]
    public void Should_evaluate_object_initializer() {
        var expr = ExpressionOnNone.Of(() => new Cat { Name = "Garfield" });

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equivalent(new Cat { Name = "Garfield" }, value);
    }

    [Fact]
    public void Should_evaluate_nested_object_initializer() {
        var expr = ExpressionOnNone.Of(() => new InitializerFixture { NestedObject = { Name = "Garfield" } });

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equivalent(new InitializerFixture { NestedObject = { Name = "Garfield" } }, value);
    }

    [Fact]
    public void Should_evaluate_nested_collection_initializer() {
        var expr = ExpressionOnNone.Of(() => new InitializerFixture { NestedCollection = { "foo" } });

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equivalent(new InitializerFixture { NestedCollection = { "foo" } }, value);
    }

    [Fact]
    public void Should_evaluate_indexer() {
        var expr = ExpressionOnNone.Of(() => new List<string> { "foo" }[0]);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equal("foo", value);
    }

    [Fact]
    public void Should_evaluate_array_index() {
        var expr = ExpressionOn<IInterpolationContext<string[]>>.Of(x => x.Data[0]);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(new[] { "foo" }, expr.Body, out var value));
        Assert.Equal("foo", value);
    }

    [Fact]
    public void Should_evaluate_array_length() {
        var expr = ExpressionOn<IInterpolationContext<string[]>>.Of(x => x.Data.Length);

        Assert.True(ReflectivePartialSplicedExpressionEvaluator.Instance.TryEvaluate(new string[3], expr.Body, out var value));
        Assert.Equal(3, value);
    }
}
