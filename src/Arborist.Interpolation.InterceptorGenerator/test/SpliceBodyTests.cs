namespace Arborist.Interpolation.InterceptorGenerator;

public class SpliceBodyTests {
    [Fact]
    public void Should_work_for_Func2_provided_via_data() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var ownerExpr = ExpressionOn<Owner>.Of(o => o.Name == ""Jon"");
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
