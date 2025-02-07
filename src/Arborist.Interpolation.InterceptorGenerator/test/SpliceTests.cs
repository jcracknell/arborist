namespace Arborist.Interpolation.InterceptorGenerator;

public class SpliceTests {
    [Fact]
    public void Should_handle_untyped_expression() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Using("System.Linq.Expressions")
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => x.Splice<int>(Expression.Constant(3)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => __t0.Coerce(global::System.Linq.Expressions.Expression.Constant(3)) switch {
                        var __v0 => (__e0.Type == __v0.Type
                        ?   __v0
                        :   global::System.Linq.Expressions.Expression.Convert(
                                __v0,
                                __e0.Type
                            ))
                    }
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_typed_lambda() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var expr = ExpressionOn<Cat>.Of(c => c.Name);
            ExpressionOnNone.Interpolate(expr, x => x.Splice<int>(x.Data));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => __t0.Coerce(__data) switch {
                        var __v0 => (__e0.Type == __v0.Type
                        ?   __v0
                        :   global::System.Linq.Expressions.Expression.Convert(
                                __v0,
                                __e0.Type
                            ))
                    }
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
