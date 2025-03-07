namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitorTests {
    [Fact]
    public void Should_work_for_as() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Cat)!, x => x.SpliceConstant(x.Data as IFormattable));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.UnaryExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (__data as global::System.IFormattable)
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_is() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Cat)!, x => x.SpliceConstant(x.Data is IFormattable));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.TypeBinaryExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (__data is global::System.IFormattable)
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
