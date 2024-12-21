using Xunit;

namespace Arborist.CodeGen;

public class SpliceBodyTests {
    [Fact]
    public void Should_work_for_Func2_provided_via_data() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
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
                __data.OwnerPredicate switch {
                    var __v0 => global::Arborist.ExpressionHelper.Replace(
                        __v0.Body,
                        global::Arborist.Internal.Collections.SmallDictionary.Create(
                            new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                __v0.Parameters[0],
                                global::System.Linq.Expressions.Expression.Property(
                                    __p0,
                                    typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Owner"")!
                                )
                            )
                        )
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_Func2_provided_as_literal() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(
                (x, c) => x.SpliceBody(c.Owner, o => o.Name == ""Jon"")
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                __t0.Coerce((o) => (o.Name == ""Jon"")) switch {
                    var __v0 => global::Arborist.ExpressionHelper.Replace(
                        __v0.Body,
                        global::Arborist.Internal.Collections.SmallDictionary.Create(
                            new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                __v0.Parameters[0],
                                global::System.Linq.Expressions.Expression.Property(
                                    __p0,
                                    typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Owner"")!
                                )
                            )
                        )
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
