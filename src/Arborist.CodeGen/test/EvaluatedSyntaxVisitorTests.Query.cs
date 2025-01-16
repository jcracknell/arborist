using Xunit;

namespace Arborist.CodeGen;

public partial class EvaluatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_transparent_identifier_in_from_clause() {
        var builder = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from a in x.Data.Cats
                from b in x.Data.Cats
                from c in x.Data.Cats
                from d in x.Data.Cats
                select b
            ));
        ");

        Assert.Equal(1, builder.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.SelectMany(
                        global::System.Linq.Enumerable.SelectMany(
                            global::System.Linq.Enumerable.SelectMany(
                                __data.Cats,
                                (a) => __data.Cats,
                                (a, b) => new { a, b }
                            ),
                            (__v0) => __data.Cats,
                            (__v0, c) => new { __v0, c }
                        ),
                        (__v1) => __data.Cats,
                        (__v1, d) => __v1.__v0.b
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::Arborist.TestFixtures.Cat>)
                )
            ",
            actual: builder.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_join_clause() {
        var builder = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from a in x.Data.Cats
                join b in x.Data.Cats on a.Id equals b.Id
                join c in x.Data.Cats on a.Id equals c.Id
                join d in x.Data.Cats on a.Id equals d.Id
                select a
            ));
        ");

        Assert.Equal(1, builder.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Join(
                        global::System.Linq.Enumerable.Join(
                            global::System.Linq.Enumerable.Join(
                                __data.Cats,
                                __data.Cats,
                                (a) => a.Id,
                                (b) => b.Id,
                                (a, b) => new { a, b }
                            ),
                            __data.Cats,
                            (__v0) => __v0.a.Id,
                            (c) => c.Id,
                            (__v0, c) => new { __v0, c }
                        ),
                        __data.Cats,
                        (__v1) => __v1.__v0.a.Id,
                        (d) => d.Id,
                        (__v1, d) => __v1.__v0.a
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::Arborist.TestFixtures.Cat>)
                )
            ",
            actual: builder.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_let_clause() {
        var builder = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                let n = c.Name
                let i = c.Id
                select n
            ));
        ");

        Assert.Equal(1, builder.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Select(
                        global::System.Linq.Enumerable.Select(
                            global::System.Linq.Enumerable.Select(
                                __data.Cats,
                                (c) => new { c, n = c.Name }
                            ),
                            (__v0) => new { __v0, i = __v0.c.Id }
                        ),
                        (__v1) => __v1.__v0.n
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.String>)
                )
            ",
            actual: builder.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
