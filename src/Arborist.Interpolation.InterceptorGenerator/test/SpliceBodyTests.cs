namespace Arborist.Interpolation.InterceptorGenerator;

public class SpliceBodyTests {
    [Fact]
    public void Should_work_for_Func1_provided_via_data() {
        // This test case is necessary because the call to replace is no longer required (and you cannot rely
        // on type inferral in the SmallDictionary construction)
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                new { Thunk = ExpressionOnNone.Of(() => 41) },
                x => x.SpliceBody(x.Data.Thunk) + 1
            );
        ");
        
        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.BinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeBinary(
                        __e0.NodeType,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Left) switch {
                            var __e1 => __data.Thunk switch {
                                var __v0 => __v0.Body
                            }
                        },
                        __e0.Right,
                        __e0.IsLiftedToNull,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
    
    [Fact]
    public void Should_work_for_Func1_provided_as_literal() {
        // This test case is necessary because the call to replace is no longer required (and you cannot rely
        // on type inferral in the SmallDictionary construction)
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                x => x.SpliceBody(() => 41) + 1
            );
        ");
        
        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.BinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeBinary(
                        __e0.NodeType,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Left) switch {
                            var __e1 => __t0.Coerce(() => 41) switch {
                                var __v0 => __v0.Body
                            }
                        },
                        __e0.Right,
                        __e0.IsLiftedToNull,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_Func2_provided_via_data() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                new { OwnerPredicate = ExpressionOn<Owner>.Of(o => o.Name == ""Jon"") },
                (x, c) => x.SpliceBody(c.Owner, x.Data.OwnerPredicate)
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => __data.OwnerPredicate switch {
                        var __v0 => global::Arborist.ExpressionHelper.Replace(
                            __v0.Body,
                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                    __v0.Parameters[0],
                                    __e0.Arguments[0]
                                )
                            )
                        )
                    }
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_Func2_provided_as_literal() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object),
                (x, c) => x.SpliceBody(c.Owner, o => o.Name == ""Jon"")
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => __t0.Coerce((o) => (o.Name == ""Jon"")) switch {
                        var __v0 => global::Arborist.ExpressionHelper.Replace(
                            __v0.Body,
                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                    __v0.Parameters[0],
                                    __e0.Arguments[0]
                                )
                            )
                        )
                    }
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
