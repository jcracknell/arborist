using Arborist.TestFixtures;
using System.Reflection;

namespace Arborist.Interpolation.Internal;

public class ReflectiveSplicedExpressionEvaluatorTests {
    private static string StaticProperty { get; } = "foo";
    private static readonly string StaticField = "foo";
    private static string StaticMethod(string argument) => argument;
    private string InstanceMethod(string argument) => argument;

    [Fact]
    public void Should_evaluate_InterpolationContext_data_access() {
        var access = Expression.Property(
            Expression.Parameter(typeof(IInterpolationContext<int>)),
            nameof(IInterpolationContext<int>.Data)
        );

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(42, access, out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Should_evaluate_constant() {
        var constant = Expression.Constant(42);

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), constant, out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void Should_evaluate_instance_field() {
        var instance = new MemberFixture { InstanceField = "foo" };
        var field = Expression.Field(Expression.Constant(instance), nameof(instance.InstanceField));

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), field, out var value));
        Assert.Equal(instance.InstanceField, value);
    }

    [Fact]
    public void Should_evaluate_static_field() {
        var field = Expression.Field(
            null,
            typeof(ReflectiveSplicedExpressionEvaluatorTests).GetField(
                nameof(StaticField),
                BindingFlags.Static | BindingFlags.NonPublic
            )!
        );

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), field, out var value));
        Assert.Equal(StaticField, value);
    }

    [Fact]
    public void Should_evaluate_instance_property() {
        var instance = new MemberFixture { InstanceProperty = "foo" };
        var property = Expression.Property(Expression.Constant(instance), nameof(instance.InstanceProperty));

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), property, out var value));
        Assert.Equal(instance.InstanceProperty, value);
    }

    [Fact]
    public void Should_evaluate_static_property() {
        var property = Expression.Property(
            null,
            typeof(ReflectiveSplicedExpressionEvaluatorTests).GetProperty(
                nameof(StaticProperty),
                BindingFlags.Static | BindingFlags.NonPublic
            )!
        );

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), property, out var value));
        Assert.Equal(StaticProperty, value);
    }

    [Fact]
    public void Should_evaluate_instance_method_call() {
        var call = Expression.Call(
            Expression.Constant(this),
            typeof(ReflectiveSplicedExpressionEvaluatorTests).GetMethod(
                nameof(InstanceMethod),
                BindingFlags.Instance | BindingFlags.NonPublic
            )!,
            Expression.Constant("foo")
        );

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), call, out var value));
        Assert.Equal(InstanceMethod("foo"), value);
    }

    [Fact]
    public void Should_evaluate_static_method_call() {
        var call = Expression.Call(
            typeof(ReflectiveSplicedExpressionEvaluatorTests).GetMethod(
                nameof(StaticMethod),
                BindingFlags.Static | BindingFlags.NonPublic
            )!,
            Expression.Constant("foo")
        );

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), call, out var value));
        Assert.Equal(StaticMethod("foo"), value);
    }

    [Fact]
    public void Should_evaluate_quoted_lambda_expression() {
        var lambda = ExpressionOn<Cat>.Of(c => c.Name);
        var quoted = Expression.Quote(lambda);

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), quoted, out var value));
        Assert.Equal(lambda, value);
    }

    [Fact]
    public void Should_evaluate_numeric_conversion() {
        var convert = Expression.Convert(Expression.Constant(42), typeof(long));

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), convert, out var value));
        Assert.Equal(42L, value);
    }

    [Fact]
    public void Should_evaluate_reference_conversion() {
        var convert = Expression.Convert(Expression.Constant("foo"), typeof(object));

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), convert, out var value));
        Assert.Equal("foo", value);
    }

    [Fact]
    public void Should_evaluate_new() {
        var newExpr = Expression.New(
            typeof(string).GetConstructor(new[] { typeof(char), typeof(int) })!,
            Expression.Constant('a'),
            Expression.Constant(3)
        );

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), newExpr, out var value));
        Assert.Equal("aaa", value);
    }

    [Fact]
    public void Should_evaluate_collection_initializer() {
        var expr = ExpressionOnNone.Of(() => new List<string> { "foo" });

        Assert.True(ReflectiveSplicedExpressionEvaluator.Instance.TryEvaluate(default(object), expr.Body, out var value));
        Assert.Equivalent(new List<string> { "foo" }, value);
    }
}
