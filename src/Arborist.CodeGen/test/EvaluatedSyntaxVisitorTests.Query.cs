using Xunit;

namespace Arborist.CodeGen;

public partial class EvaluatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_select_clause() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                select c.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Select(
                        __data.Cats,
                        (c) => c.Name
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.String>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_group_clause() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                group c by c.Age
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.GroupBy(
                        __data.Cats,
                        (c) => c.Age,
                        (c) => c
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<
                        global::System.Linq.IGrouping<
                            global::System.Nullable<global::System.Int32>,
                            global::Arborist.TestFixtures.Cat
                        >
                    >)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_group_into() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                group c by c.Age
                into ageGroup
                select ageGroup.Count()
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Select(
                        global::System.Linq.Enumerable.GroupBy(
                            __data.Cats,
                            (c) => c.Age,
                            (c) => c
                        ),
                        (ageGroup) => global::System.Linq.Enumerable.Count(ageGroup)
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.Int32>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_join_clause() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                join c1 in x.Data.Cats on c.Id equals c1.Id
                select c1.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Join(
                        __data.Cats,
                        __data.Cats,
                        (c) => c.Id,
                        (c1) => c1.Id,
                        (c, c1) => c1.Name
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.String>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_non_final_join_clause() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                join c1 in x.Data.Cats on c.Id equals c1.Id
                where c.Age == 8
                select c1.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Select(
                        global::System.Linq.Enumerable.Where(
                            global::System.Linq.Enumerable.Join(
                                __data.Cats,
                                __data.Cats,
                                (c) => c.Id,
                                (c1) => c1.Id,
                                (c, c1) => new { c, c1 }
                            ),
                            (__v0) => (__v0.c.Age == 8)
                        ),
                        (__v0) => __v0.c1.Name
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.String>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_join_into_clause() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                join c1 in x.Data.Cats on c.Id equals c1.Id into cs
                from cc in cs
                select cc.Age
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.SelectMany(
                        global::System.Linq.Enumerable.GroupJoin(
                            __data.Cats,
                            __data.Cats,
                            (c) => c.Id,
                            (c1) => c1.Id,
                            (c, cs) => new { c, cs }
                        ),
                        (__v0) => __v0.cs,
                        (__v0, cc) => cc.Age
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.Nullable<global::System.Int32>>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_let_clause() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                let name = c.Name
                select name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Select(
                        global::System.Linq.Enumerable.Select(
                            __data.Cats,
                            (c) => new { c, name = c.Name }
                        ),
                        (__v0) => __v0.name
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.String>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_where_clause() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceValue(
                from c in x.Data.Cats
                where c.Age == 8
                select c.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::System.Linq.Enumerable.Select(
                        global::System.Linq.Enumerable.Where(
                            __data.Cats,
                            (c) => (c.Age == 8)
                        ),
                        (c) => c.Name
                    ),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.String>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
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
