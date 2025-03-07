namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_select_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                select c.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Select(
                                __data.Cats,
                                (c) => c.Name
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
    public void Should_handle_from_clause_with_cast() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from object c in x.Data.Cats
                select c.GetHashCode()
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Select(
                                global::System.Linq.Enumerable.Cast<global::System.Object>(
                                    __data.Cats
                                ),
                                (c) => c.GetHashCode()
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
    public void Should_handle_subsequent_from_clause_with_cast() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                from object o in c.Owner.Cats
                select o.GetHashCode()
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.SelectMany(
                                __data.Cats,
                                (c) => global::System.Linq.Enumerable.Cast<global::System.Object>(
                                    c.Owner.Cats
                                ),
                                (c, o) => o.GetHashCode()
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
    public void Should_handle_group_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                group c by c.Age
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.GroupBy(
                                __data.Cats,
                                (c) => c.Age
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
    public void Should_handle_group_into() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                group c by c.Age
                into ageGroup
                select ageGroup.Count()
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Select(
                                global::System.Linq.Enumerable.GroupBy(
                                    __data.Cats,
                                    (c) => c.Age
                                ),
                                (ageGroup) => global::System.Linq.Enumerable.Count(ageGroup)
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
    public void Should_handle_join_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                join c1 in x.Data.Cats on c.Id equals c1.Id
                select c1.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Join(
                                __data.Cats,
                                __data.Cats,
                                (c) => c.Id,
                                (c1) => c1.Id,
                                (c, c1) => c1.Name
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
    public void Should_handle_join_clause_with_cast() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceConstant(
                from c in Array.Empty<Cat>()
                join object c1 in Array.Empty<Cat>() on c.Id equals c1.GetHashCode()
                select c.GetHashCode()
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Join(
                                global::System.Array.Empty<global::Arborist.TestFixtures.Cat>(),
                                global::System.Linq.Enumerable.Cast<global::System.Object>(
                                    global::System.Array.Empty<global::Arborist.TestFixtures.Cat>()
                                ),
                                (c) => c.Id,
                                (c1) => c1.GetHashCode(),
                                (c, c1) => c.GetHashCode()
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
    public void Should_handle_non_final_join_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                join c1 in x.Data.Cats on c.Id equals c1.Id
                where c.Age == 8
                select c1.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Select(
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
    public void Should_handle_join_into_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                join c1 in x.Data.Cats on c.Id equals c1.Id into cs
                from cc in cs
                select cc.Age
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.SelectMany(
                                global::System.Linq.Enumerable.GroupJoin(
                                    __data.Cats,
                                    __data.Cats,
                                    (c) => c.Id,
                                    (c1) => c1.Id,
                                    (c, cs) => new { c, cs }
                                ),
                                (__v0) => __v0.cs,
                                (__v0, cc) => cc.Age
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
    public void Should_handle_let_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                let name = c.Name
                select name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Select(
                                global::System.Linq.Enumerable.Select(
                                    __data.Cats,
                                    (c) => new { c, name = c.Name }
                                ),
                                (__v0) => __v0.name
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
    public void Should_handle_where_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                where c.Age == 8
                select c.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Select(
                                global::System.Linq.Enumerable.Where(
                                    __data.Cats,
                                    (c) => (c.Age == 8)
                                ),
                                (c) => c.Name
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
    public void Should_handle_transparent_identifier_in_from_clause() {
        var builder = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
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
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.SelectMany(
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
                            )
                        },
                        __e0.Type
                    )
                }
            ",
            actual: builder.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_join_clause() {
        var builder = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
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
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Join(
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
                            )
                        },
                        __e0.Type
                    )
                }
            ",
            actual: builder.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_transparent_identifier_in_let_clause() {
        var builder = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Owner)!, x => x.SpliceConstant(
                from c in x.Data.Cats
                let n = c.Name
                let i = c.Id
                select n
            ));
        ");

        Assert.Equal(1, builder.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Enumerable.Select(
                                global::System.Linq.Enumerable.Select(
                                    global::System.Linq.Enumerable.Select(
                                        __data.Cats,
                                        (c) => new { c, n = c.Name }
                                    ),
                                    (__v0) => new { __v0, i = __v0.c.Id }
                                ),
                                (__v1) => __v1.__v0.n
                            )
                        },
                        __e0.Type
                    )
                }
            ",
            actual: builder.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
