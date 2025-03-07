namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_implicit_array_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                x => x.SpliceConstant(new[] { ""foo"", ""bar"" })
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.NewArrayExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new[] { ""foo"", ""bar"" }
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_array_initializer_with_explicit_dimensions() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                x => x.SpliceConstant(new string[2] { ""foo"", ""bar"" })
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.NewArrayExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::System.String[] { ""foo"", ""bar"" }
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_array_with_explicit_dimensions() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                x => x.SpliceConstant(new string[3, 42])
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.NewArrayExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::System.String[3,42]
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_nested_array_with_explicit_dimensions() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                x => x.SpliceConstant(new string[2, 42][])
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.NewArrayExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::System.String[2, 42][]
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_object_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceConstant(new Cat { Name = ""Garfield"" }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberInitExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::Arborist.TestFixtures.Cat() { Name = ""Garfield"" }
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_collection_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceConstant(new List<string> { ""foo"", ""bar"" }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.ListInitExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::System.Collections.Generic.List<global::System.String>() {
                                ""foo"",
                                ""bar""
                            }
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_dictionary_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceConstant(new Dictionary<string, int> {
                { ""foo"", 1 },
                { ""bar"", 2 }
            }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.ListInitExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>(){
                                { ""foo"", 1 },
                                { ""bar"", 2 }
                            }
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_nested_object_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x =>
                x.SpliceConstant(new Cat { Owner = { Name = ""Jon"" } })
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberInitExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::Arborist.TestFixtures.Cat() { Owner = { Name = ""Jon"" } }
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_nested_collection_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x =>
                x.SpliceConstant(new NestedCollectionInitializerFixture<string> {
                    List = { ""foo"" },
                    Dictionary = { { ""bar"", ""baz"" } }
                })
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberInitExpression)(__e0.Arguments[0]) switch {
                            var __e1 => new global::Arborist.TestFixtures.NestedCollectionInitializerFixture<global::System.String>(){
                                List = { ""foo"" },
                                Dictionary = { { ""bar"", ""baz"" } }
                            }
                        },
                        __e0.Type
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
