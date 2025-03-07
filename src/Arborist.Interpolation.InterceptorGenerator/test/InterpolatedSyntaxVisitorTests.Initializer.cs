namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_array_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object),
                x => new[] { x.SpliceConstant(""foo""), ""bar"", x.SpliceConstant(""baz"") }
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
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => ""foo""
                                    },
                                    __e1.Type
                                )
                            },
                            __e0.Expressions[1],
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expressions[2]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => ""baz""
                                    },
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
                x => new string[2] { x.SpliceConstant(""foo""), ""bar"" }
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
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => ""foo""
                                    },
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
                x => new string[x.SpliceConstant(3), 42]
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
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => 3
                                    },
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
                x => new Cat { Name = x.SpliceConstant(""Garfield"") }
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
                                            (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                                var __e3 => ""Garfield""
                                            },
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
            ExpressionOnNone.Interpolate(x => new List<string> { x.SpliceConstant(""foo""), ""bar"" });
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
                                                (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                                    var __e3 => ""foo""
                                                },
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
                { x.SpliceConstant(""foo""), 1 }
                { ""bar"", x.SpliceConstant(2) }
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
                                                (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                                    var __e3 => ""foo""
                                                },
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
                                                (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                                    var __e3 => 2
                                                },
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
            ExpressionOnNone.Interpolate(x => new Owner { Cats = new List<Cat> { x.SpliceConstant(default(Cat)!) } });
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
                                                                    (global::System.Linq.Expressions.ConstantExpression)(__e4.Arguments[0]) switch {
                                                                        var __e5 => default(global::Arborist.TestFixtures.Cat)!
                                                                    },
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
            ExpressionOnNone.Interpolate(x => new List<Cat> { new Cat { Name = x.SpliceConstant(""Garfield"") } });
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
                                                                    (global::System.Linq.Expressions.ConstantExpression)(__e4.Arguments[0]) switch {
                                                                        var __e5 => ""Garfield""
                                                                    },
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
                new Cat { Owner = { Name = x.SpliceConstant(""Jon"") } }
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
                                                        (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                            var __e4 => ""Jon""
                                                        },
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
                    List = { x.SpliceConstant(""foo"") },
                    Dictionary = { { x.SpliceConstant(""bar""), x.SpliceConstant(""baz"") } }
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
                                                            (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                                var __e4 => ""foo""
                                                            },
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
                                                            (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                                var __e4 => ""bar""
                                                            },
                                                            __e3.Type
                                                        )
                                                    },
                                                    (global::System.Linq.Expressions.MethodCallExpression)(__e2.Arguments[1]) switch {
                                                        var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                            (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                                var __e4 => ""baz""
                                                            },
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
