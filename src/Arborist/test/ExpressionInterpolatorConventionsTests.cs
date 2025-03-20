using Arborist.Interpolation;
using Arborist.Interpolation.Internal;
using System.Reflection;

namespace Arborist;

public class ExpressionInterpolatorConventionsTests {
    [Fact]
    public void Methods_with_ExpressionInterpolatorAttribute_should_conform() {
        foreach(var method in (
            from t in typeof(ExpressionHelper).Assembly.GetTypes()
            from m in t.GetMethods()
            where m.IsDefined(typeof(ExpressionInterpolatorAttribute), inherit: true)
            select m
        )) {
            if(!method.Name.Contains("Interpolate"))
                Assert.Fail($"Interpolator {method} has an invalid name.");

            var dataParameters = method.GetParameters()
            .Where(p => p.IsDefined(typeof(InterpolatedDataParameterAttribute)))
            .ToList();

            var expressionParameters = method.GetParameters()
            .Where(p => p.IsDefined(typeof(InterpolatedExpressionParameterAttribute)))
            .ToList();

            if(dataParameters.Count > 1)
                Assert.Fail($"Interpolator {method} has multiple parameters with {nameof(InterpolatedDataParameterAttribute)}.");

            if(expressionParameters.Count == 0)
                Assert.Fail($"Interpolator {method} has no parameters marked with {nameof(InterpolatedExpressionParameterAttribute)}.");

            foreach(var parameter in expressionParameters) {
                if(!IsInterpolationExpression(parameter.ParameterType, out var contextType))
                    continue;

                if(!parameter.IsDefined(typeof(InterpolatedExpressionParameterAttribute)))
                    Assert.Fail($"Interpolator {method} has parameter {parameter} which is an interpolated expression, but is not marked with {nameof(InterpolatedExpressionParameterAttribute)}.");

                if(dataParameters.Count == 0 && contextType != typeof(IInterpolationContext))
                    Assert.Fail($"Interpolator {method} has parameter {parameter} with incorrect context type {contextType}.");

                if(dataParameters.Count == 1 && contextType != typeof(IInterpolationContext<>).MakeGenericType(dataParameters[0].ParameterType))
                    Assert.Fail($"Interpolator {method} has parameter {parameter} with incorrect context type {contextType}.");
            }
        }
    }

    [Fact]
    public void Parameters_with_InterpolatedExpressionParameterAttribute_should_conform() {
        foreach(var parameter in (
            from t in typeof(ExpressionHelper).Assembly.GetTypes()
            from m in t.GetMethods()
            from p in m.GetParameters()
            where p.IsDefined(typeof(InterpolatedExpressionParameterAttribute))
            select p
        )) {
            if(!parameter.Member.IsDefined(typeof(ExpressionInterpolatorAttribute), inherit: true))
                Assert.Fail($"Parameter {parameter} with {nameof(InterpolatedExpressionParameterAttribute)} is defined in member {parameter.Member} without {nameof(ExpressionInterpolatorAttribute)}.");

            if(!IsExpression(parameter.ParameterType))
                Assert.Fail($"Parameter {parameter} with {nameof(InterpolatedExpressionParameterAttribute)} is not an {typeof(Expression<>)}.");

            if(!IsInterpolationExpression(parameter.ParameterType, out _))
                Assert.Fail($"Parameter {parameter} with {nameof(InterpolatedExpressionParameterAttribute)} does not accept an {typeof(IInterpolationContext)}.");
        }
    }

    private static bool IsExpression(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Expression<>);

    private static bool IsInterpolationExpression(Type type, [NotNullWhen(true)] out Type? contextType) {
        if(IsExpression(type)) {
            contextType = type.GenericTypeArguments[0].GenericTypeArguments[0];
            if(contextType == typeof(IInterpolationContext))
                return true;
            if(contextType.IsGenericType && contextType.GetGenericTypeDefinition() == typeof(IInterpolationContext<>))
                return true;
        }

        contextType = default;
        return false;
    }
}
