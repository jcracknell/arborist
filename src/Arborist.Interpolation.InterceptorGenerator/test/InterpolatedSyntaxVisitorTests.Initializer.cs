namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_array_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object),
                x => new[] { x.SpliceValue(""foo""), ""bar"", x.SpliceValue(""baz"") }
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
            (global::System.Linq.Expressions.NewArrayExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.NewArrayInit(
                        __e0.Type.GetElementType()!,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expressions[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    ""foo"",
                                    __e1.Type
                                )
                            },
                            __e0.Expressions[1],
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expressions[2]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    ""baz"",
                                    __e1.Type
                                )
                            }
                        }
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
                x => new string[2] { x.SpliceValue(""foo""), ""bar"" }
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.NewArrayExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.NewArrayInit(
                        __e0.Type.GetElementType()!,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expressions[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    ""foo"",
                                    __e1.Type
                                )
                            },
                            __e0.Expressions[1]
                        }
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
                x => new string[x.SpliceValue(3), 42]
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.NewArrayExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.NewArrayBounds(
                        __e0.Type.GetElementType()!,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expressions[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    3,
                                    __e1.Type
                                )
                            },
                            __e0.Expressions[1]
                        }
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
            ExpressionOnNone.Interpolate(default(object),
                x => new Cat { Name = x.SpliceValue(""Garfield"") }
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MemberInitExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MemberInit(
                        __e0.NewExpression,
                        new global::System.Linq.Expressions.MemberBinding[] {
                            (global::System.Linq.Expressions.MemberAssignment)(__e0.Bindings[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Bind(
                                    __e1.Member,
                                    (global::System.Linq.Expressions.MethodCallExpression)(__e1.Expression) switch {
                                        var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                            ""Garfield"",
                                            __e2.Type
                                        )
                                    }
                                )
                            }
                        }
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
            ExpressionOnNone.Interpolate(x => new List<string> { x.SpliceValue(""foo""), ""bar"" });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.ListInitExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.ListInit(
                        __e0.NewExpression,
                        new global::System.Linq.Expressions.ElementInit[] {
                            (global::System.Linq.Expressions.ElementInit)(__e0.Initializers[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.ElementInit(
                                    __e1.AddMethod,
                                    new global::System.Linq.Expressions.Expression[] {
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[0]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                                ""foo"",
                                                __e2.Type
                                            )
                                        }
                                    }
                                )
                            },
                            __e0.Initializers[1]
                        }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_multi_arg_collection_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new Dictionary<string, int> {
                { x.SpliceValue(""foo""), 1 }
                { ""bar"", x.SpliceValue(2) }
            });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.ListInitExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.ListInit(
                        __e0.NewExpression,
                        new global::System.Linq.Expressions.ElementInit[] {
                            (global::System.Linq.Expressions.ElementInit)(__e0.Initializers[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.ElementInit(
                                    __e1.AddMethod,
                                    new global::System.Linq.Expressions.Expression[] {
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[0]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                                ""foo"",
                                                __e2.Type
                                            )
                                        },
                                        __e1.Arguments[1]
                                    }
                                )
                            },
                            (global::System.Linq.Expressions.ElementInit)(__e0.Initializers[1]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.ElementInit(
                                    __e1.AddMethod,
                                    new global::System.Linq.Expressions.Expression[] {
                                        __e1.Arguments[0],
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[1]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                                2,
                                                __e2.Type
                                            )
                                        }
                                    }
                                )
                            }
                        }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );

    }

    [Fact]
    public void Should_handle_collection_initializer_in_object_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new Owner { Cats = new List<Cat> { x.SpliceValue(default(Cat)!) } });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MemberInitExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MemberInit(
                        __e0.NewExpression,
                        new global::System.Linq.Expressions.MemberBinding[] {
                            (global::System.Linq.Expressions.MemberAssignment)(__e0.Bindings[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Bind(
                                    __e1.Member,
                                    (global::System.Linq.Expressions.ListInitExpression)(__e1.Expression) switch {
                                        var __e2 => global::System.Linq.Expressions.Expression.ListInit(
                                            __e2.NewExpression,
                                            new global::System.Linq.Expressions.ElementInit[] {
                                                (global::System.Linq.Expressions.ElementInit)(__e2.Initializers[0]) switch {
                                                    var __e3 => global::System.Linq.Expressions.Expression.ElementInit(
                                                        __e3.AddMethod,
                                                        new global::System.Linq.Expressions.Expression[] {
                                                            (global::System.Linq.Expressions.MethodCallExpression)(__e3.Arguments[0]) switch {
                                                                var __e4 => global::System.Linq.Expressions.Expression.Constant(
                                                                    default(global::Arborist.TestFixtures.Cat)!,
                                                                    __e4.Type
                                                                )
                                                            }
                                                        }
                                                    )
                                                }
                                            }
                                        )
                                    }
                                )
                            }
                        }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_object_initializer_in_collection_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new List<Cat> { new Cat { Name = x.SpliceValue(""Garfield"") } });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.ListInitExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.ListInit(
                        __e0.NewExpression,
                        new global::System.Linq.Expressions.ElementInit[] {
                            (global::System.Linq.Expressions.ElementInit)(__e0.Initializers[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.ElementInit(
                                    __e1.AddMethod,
                                    new global::System.Linq.Expressions.Expression[] {
                                        (global::System.Linq.Expressions.MemberInitExpression)(__e1.Arguments[0]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.MemberInit(
                                                __e2.NewExpression,
                                                new global::System.Linq.Expressions.MemberBinding[] {
                                                    (global::System.Linq.Expressions.MemberAssignment)(__e2.Bindings[0]) switch {
                                                        var __e3 => global::System.Linq.Expressions.Expression.Bind(
                                                            __e3.Member,
                                                            (global::System.Linq.Expressions.MethodCallExpression)(__e3.Expression) switch {
                                                                var __e4 => global::System.Linq.Expressions.Expression.Constant(
                                                                    ""Garfield"",
                                                                    __e4.Type
                                                                )
                                                            }
                                                        )
                                                    }
                                                }
                                            )
                                        }
                                    }
                                )
                            }
                        }
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
                new Cat { Owner = { Name = x.SpliceValue(""Jon"") } }
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MemberInitExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MemberInit(
                        __e0.NewExpression,
                        new global::System.Linq.Expressions.MemberBinding[] {
                            (global::System.Linq.Expressions.MemberMemberBinding)(__e0.Bindings[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.MemberBind(
                                    __e1.Member,
                                    new global::System.Linq.Expressions.MemberBinding[] {
                                        (global::System.Linq.Expressions.MemberAssignment)(__e1.Bindings[0]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.Bind(
                                                __e2.Member,
                                                (global::System.Linq.Expressions.MethodCallExpression)(__e2.Expression) switch {
                                                    var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                        ""Jon"",
                                                        __e3.Type
                                                    )
                                                }
                                            )
                                        }
                                    }
                                )
                            }
                        }
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
                new NestedCollectionInitializerFixture<string> {
                    List = { x.SpliceValue(""foo"") },
                    Dictionary = { { x.SpliceValue(""bar""), x.SpliceValue(""baz"") } }
                }
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MemberInitExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MemberInit(
                        __e0.NewExpression,
                        new global::System.Linq.Expressions.MemberBinding[] {
                            (global::System.Linq.Expressions.MemberListBinding)(__e0.Bindings[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.ListBind(
                                    __e1.Member,
                                    new global::System.Linq.Expressions.ElementInit[] {
                                        (global::System.Linq.Expressions.ElementInit)(__e1.Initializers[0]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.ElementInit(
                                                __e2.AddMethod,
                                                new global::System.Linq.Expressions.Expression[] {
                                                    (global::System.Linq.Expressions.MethodCallExpression)(__e2.Arguments[0]) switch {
                                                        var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                            ""foo"",
                                                            __e3.Type
                                                        )
                                                    }
                                                }
                                            )
                                        }
                                    }
                                )
                            },
                            (global::System.Linq.Expressions.MemberListBinding)(__e0.Bindings[1]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.ListBind(
                                    __e1.Member,
                                    new global::System.Linq.Expressions.ElementInit[] {
                                        (global::System.Linq.Expressions.ElementInit)(__e1.Initializers[0]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.ElementInit(
                                                __e2.AddMethod,
                                                new global::System.Linq.Expressions.Expression[] {
                                                    (global::System.Linq.Expressions.MethodCallExpression)(__e2.Arguments[0]) switch {
                                                        var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                            ""bar"",
                                                            __e3.Type
                                                        )
                                                    },
                                                    (global::System.Linq.Expressions.MethodCallExpression)(__e2.Arguments[1]) switch {
                                                        var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                            ""baz"",
                                                            __e3.Type
                                                        )
                                                    }
                                                }
                                            )
                                        }
                                    }
                                )
                            }
                        }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
