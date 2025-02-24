namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_implicit_boxing_conversion() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate<object?, object>(default(object), x => x.SpliceValue(42));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.UnaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Convert(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Operand) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => 42
                                },
                                __e1.Type
                            )
                        },
                        __e0.Type,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_implicit_numeric_conversion() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate<object?, decimal>(default(object), x => x.SpliceValue(42));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.UnaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Convert(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Operand) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => 42
                                },
                                __e1.Type
                            )
                        },
                        __e0.Type,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_implicit_user_defined_conversion() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate<object?, ImplicitlyConvertible<string>>(default(object), x => x.SpliceValue(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.UnaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Convert(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Operand) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => ""foo""
                                },
                                __e1.Type
                            )
                        },
                        __e0.Type,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
