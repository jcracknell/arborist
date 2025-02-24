namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_work_for_i32_add() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate((x, o) => o.Id + x.SpliceValue(42));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.BinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeBinary(
                        __e0.NodeType,
                        __e0.Left,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Right) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => 42
                                },
                                __e1.Type
                            )
                        },
                        __e0.IsLiftedToNull,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_i32_lt() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.Id < x.SpliceValue(42))
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.BinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeBinary(
                        __e0.NodeType,
                        __e0.Left,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Right) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => 42
                                },
                                __e1.Type
                            )
                        },
                        __e0.IsLiftedToNull,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_string_add() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.Name + x.SpliceValue(""bar""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.BinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeBinary(
                        __e0.NodeType,
                        __e0.Left,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Right) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => ""bar""
                                },
                                __e1.Type
                            )
                        },
                        __e0.IsLiftedToNull,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_as() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceValue<object>(default!) as IFormattable);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.UnaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.TypeAs(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Operand) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => default(global::System.Object)!
                                },
                                __e1.Type
                            )
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
            ExpressionOnNone.Interpolate(x => x.SpliceValue<object>(default!) is IFormattable);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.TypeBinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.TypeIs(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expression) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => default(global::System.Object)!
                                },
                                __e1.Type
                            )
                        },
                        __e0.TypeOperand
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
