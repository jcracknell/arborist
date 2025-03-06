namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_constructor() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new string(x.SpliceValue('0'), x.SpliceValue(3)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.NewExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.New(
                        __e0.Constructor!,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => '0'
                                    },
                                    __e1.Type
                                )
                            },
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[1]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => 3
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
    public void Should_handle_target_typed_constructor() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object),
                (x, m) => m.InstanceMethod(new(x.SpliceValue('0'), x.SpliceValue(3)))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        __e0.Object!,
                        __e0.Method,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.NewExpression)(__e0.Arguments[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.New(
                                    __e1.Constructor!,
                                    new global::System.Linq.Expressions.Expression[] {
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[0]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                                (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                                    var __e3 => '0'
                                                },
                                                __e2.Type
                                            )
                                        },
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[1]) switch {
                                            var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                                (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                                    var __e3 => 3
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
    public void Should_handle_instance_field() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceValue(default(MemberFixture)!).InstanceField);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MemberExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeMemberAccess(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expression!) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => default(global::Arborist.TestFixtures.MemberFixture)!
                                },
                                __e1.Type
                            )
                        },
                        __e0.Member
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_instance_property() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceValue(default(MemberFixture)!).InstanceProperty);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MemberExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeMemberAccess(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Expression!) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => default(global::Arborist.TestFixtures.MemberFixture)!
                                },
                                __e1.Type
                            )
                        },
                        __e0.Member
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );

    }

    [Fact]
    public void Should_handle_static_field() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => MemberFixture.StaticField + x.SpliceValue(""foo"");
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
                                    var __e2 => ""foo""
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
    public void Should_handle_static_property() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => MemberFixture.StaticProperty + x.SpliceValue(""foo""));
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
                                    var __e2 => ""foo""
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
    public void Should_handle_instance_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate((x, f) => f.InstanceMethod(x.SpliceValue(""foo"")));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        __e0.Object!,
                        __e0.Method,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => ""foo""
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
    public void Should_handle_static_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => MemberFixture.StaticMethod(x.SpliceValue(""foo"")));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        __e0.Method,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => ""foo""
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
    public void Should_handle_unary() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => !x.SpliceValue(true));
        ");


        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.UnaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeUnary(
                        __e0.NodeType,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Operand) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => true
                                },
                                __e1.Type
                            )
                        },
                        __e0.Type,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_cast() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => (decimal)x.SpliceValue(42));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.UnaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Convert(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Operand) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => 42
                                },
                                __e1.Type
                            )
                        },
                        __e0.Type,
                        __e0.Method
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_binary() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.Name + x.SpliceValue(""foo""));
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
                                    var __e2 => ""foo""
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
    public void Should_handle_ternary() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.IsAlive == x.SpliceValue(true) ? x.SpliceValue(""true"") : x.SpliceValue(""false""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.ConditionalExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Condition(
                        (global::System.Linq.Expressions.BinaryExpression)(__e0.Test) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.MakeBinary(
                                __e1.NodeType,
                                __e1.Left,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Right) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                        (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                            var __e3 => true
                                        },
                                        __e2.Type
                                    )
                                },
                                __e1.IsLiftedToNull,
                                __e1.Method
                            )
                        },
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.IfTrue) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => ""true""
                                },
                                __e1.Type
                            )
                        },
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.IfFalse) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => ""false""
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
    public void Should_handle_lambda() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate((x, o) => o.Cats.Any(c => c.IsAlive == x.SpliceValue(true)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        __e0.Method,
                        new global::System.Linq.Expressions.Expression[] {
                            __e0.Arguments[0],
                            (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[1]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                    __e1.Type,
                                    (global::System.Linq.Expressions.BinaryExpression)(__e1.Body) switch {
                                        var __e2 => global::System.Linq.Expressions.Expression.MakeBinary(
                                            __e2.NodeType,
                                            __e2.Left,
                                            (global::System.Linq.Expressions.MethodCallExpression)(__e2.Right) switch {
                                                var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                    (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                        var __e4 => true
                                                    },
                                                    __e3.Type
                                                )
                                            },
                                            __e2.IsLiftedToNull,
                                            __e2.Method
                                        )
                                    },
                                    __e1.Parameters
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
    public void Should_handle_default_value_type() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceValue(42) == default);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.BinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeBinary(
                        __e0.NodeType,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Left) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => 42
                                },
                                __e1.Type
                            )
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
    public void Should_handle_default_reference_type() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x.SpliceValue(""foo"") == default);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.BinaryExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.MakeBinary(
                        __e0.NodeType,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Left) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => ""foo""
                                },
                                __e1.Type
                            )
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
    public void Should_handle_anonymous_type() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => new {
                foo = x.SpliceValue(""foo""),
                bar = x.SpliceValue(42)
            });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.NewExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.New(
                        __e0.Constructor!,
                        new global::System.Linq.Expressions.Expression[] {
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => ""foo""
                                    },
                                    __e1.Type
                                )
                            },
                            (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[1]) switch {
                                var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                    (global::System.Linq.Expressions.ConstantExpression)(__e1.Arguments[0]) switch {
                                        var __e2 => 42
                                    },
                                    __e1.Type
                                )
                            }
                        },
                        __e0.Members
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_null_forgiving_operator() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => x.SpliceValue(x.Data)!.GetHashCode());
        ");

        // The null forgiving operator does nothing, and is omitted from the resulting expression tree
        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Object!) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Constant(
                                (global::System.Linq.Expressions.MemberExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => __data
                                },
                                __e1.Type
                            )
                        },
                        __e0.Method,
                        new global::System.Linq.Expressions.Expression[] { }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_checked() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => checked(c.Id + x.SpliceValue(42)));
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
    public void Should_work_for_unchecked() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => unchecked(c.Id + x.SpliceValue(42)));
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
}
