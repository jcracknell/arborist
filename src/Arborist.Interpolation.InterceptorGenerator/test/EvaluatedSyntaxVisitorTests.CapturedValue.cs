namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitorTests {
    // Evaluation of embedded constants is a huge pain in the butt because it's the only reason
    // we need to follow the expression tree. As such we need a test for every possible syntax
    // node to validate that the tree is followed correctly.

    [Fact]
    public void Should_handle_captured_local() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(value));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    __e1
                                )
                            ) switch {
                                var __c0 => __c0
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
    public void Should_handle_captured_instance_field_with_explicit_this() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class TestClass {
                public string Value = ""foo"";

                public void Main() {
                    ExpressionOnNone.Interpolate(x => x.SpliceValue(this.Value));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::Test.TestClass)
                                ((global::System.Linq.Expressions.ConstantExpression)
                                    __e1.Expression!
                                ).Value
                            ) switch {
                                var __c0 => __c0.Value
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
    public void Should_handle_captured_instance_field_with_implicit_this() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class TestClass {
                public string Value = ""foo"";

                public void Main() {
                    ExpressionOnNone.Interpolate(x => x.SpliceValue(Value));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::Test.TestClass)
                                ((global::System.Linq.Expressions.ConstantExpression)
                                    __e1.Expression!
                                ).Value
                            ) switch {
                                var __c0 => __c0.Value
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
    public void Should_handle_captured_local_anonymous_class_instance() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = new { Foo = ""foo"" };
            ExpressionOnNone.Interpolate(x => x.SpliceValue(value.Foo));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberExpression)(__e0.Arguments[0]) switch {
                            var __e1 => __t0.Cast(
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        __e1.Expression!
                                    )
                                )
                            ) switch {
                                var __c0 => __c0.Foo
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
    public void Should_handle_captured_local_in_binary_expression() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(value + ""bar""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.BinaryExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        __e1.Left
                                    )
                                )
                            ) switch {
                                var __c0 => (__c0 + ""bar"")
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
    public void Should_handle_captured_local_in_unary_expression() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = 42;
            ExpressionOnNone.Interpolate(x => x.SpliceValue(-value));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.UnaryExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.Int32)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        __e1.Operand
                                    )
                                )
                            ) switch {
                                var __c0 => -__c0
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
    public void Should_handle_captured_local_in_methodcallexpression() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(string.Concat(""bar"", value));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        __e1.Arguments[1]
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.String.Concat(
                                    ""bar"",
                                    __c0
                                )
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
    public void Should_handle_captured_local_in_new_expression() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var c = '0';
            var count = 42;
            ExpressionOnNone.Interpolate(x => x.SpliceValue(new string(c, count));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.NewExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (
                                ((global::System.Char)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            __e1.Arguments[0]
                                        )
                                    )
                                ),
                                ((global::System.Int32)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            __e1.Arguments[1]
                                        )
                                    )
                                )
                            ) switch {
                                var (__c0, __c1) => new global::System.String(__c0, __c1)
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
    public void Should_handle_captured_local_in_object_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""Garfield"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(new Cat { Name = value }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MemberInitExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        ((global::System.Linq.Expressions.MemberAssignment)
                                            __e1.Bindings[0]
                                        ).Expression
                                    )
                                )
                            ) switch {
                                var __c0 => new global::Arborist.TestFixtures.Cat() {
                                    Name = __c0
                                }
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
    public void Should_handle_captured_local_in_list_initializer() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var count = 1;
            var value = ""Garfield"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(new List<string>(count) { value }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.ListInitExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (
                                ((global::System.Int32)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.NewExpression)
                                                __e1.NewExpression
                                            ).Arguments[0]
                                        )
                                    )
                                ),
                                ((global::System.String)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.ElementInit)
                                                __e1.Initializers[0]
                                            ).Arguments[0]
                                        )
                                    )
                                )
                            ) switch {
                                var (__c0, __c1) => new global::System.Collections.Generic.List<global::System.String>(
                                    __c0
                                ) {
                                    __c1
                                }
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
    public void Should_handle_captured_local_in_lambda() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                default(Owner)!.Cats.Any(c => c.Name == value)
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        ((global::System.Linq.Expressions.BinaryExpression)
                                            ((global::System.Linq.Expressions.LambdaExpression)
                                                __e1.Arguments[1]
                                            ).Body
                                        ).Right
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.Linq.Enumerable.Any(
                                    default(global::Arborist.TestFixtures.Owner)!.Cats,
                                    (c) => (c.Name == __c0)
                                )
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
    public void Should_handle_captured_local_in_quoted_lambda() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                default(Owner)!.CatsQueryable.Any(c => c.Name == value)
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        ((global::System.Linq.Expressions.BinaryExpression)
                                            ((global::System.Linq.Expressions.LambdaExpression)
                                                ((global::System.Linq.Expressions.UnaryExpression)
                                                    __e1.Arguments[1]
                                                ).Operand
                                            ).Body
                                        ).Right
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.Linq.Queryable.Any(
                                    default(global::Arborist.TestFixtures.Owner)!.CatsQueryable,
                                    (c) => (c.Name == __c0)
                                )
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
    public void Should_handle_captured_local_in_nested_quoted_lambda() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                default(Owner)!.CatsQueryable.Any(c => Array.Empty<Cat>().AsQueryable().Any(c0 => c0.Name == value))
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        ((global::System.Linq.Expressions.BinaryExpression)
                                            ((global::System.Linq.Expressions.LambdaExpression)
                                                ((global::System.Linq.Expressions.UnaryExpression)
                                                    ((global::System.Linq.Expressions.MethodCallExpression)
                                                        ((global::System.Linq.Expressions.LambdaExpression)
                                                            ((global::System.Linq.Expressions.UnaryExpression)
                                                                __e1.Arguments[1]
                                                            ).Operand
                                                        ).Body
                                                    ).Arguments[1]
                                                ).Operand
                                            ).Body
                                        ).Right
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.Linq.Queryable.Any(
                                    default(global::Arborist.TestFixtures.Owner)!.CatsQueryable,
                                    (c) => global::System.Linq.Queryable.Any(
                                        global::System.Linq.Queryable.AsQueryable(
                                            global::System.Array.Empty<global::Arborist.TestFixtures.Cat>()
                                        ),
                                        (c0) => (c0.Name == __c0)
                                    )
                                )
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
    public void Should_handle_captured_local_in_from_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var cats = new[] { new Cat() };
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                from c in cats
                select c.Name
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::Arborist.TestFixtures.Cat[])
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        __e1.Arguments[0]
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.Linq.Enumerable.Select(
                                    __c0,
                                    (c) => c.Name
                                )
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
    public void Should_handle_captured_local_in_group_by_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var i = 42;
            var s = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                from o in Array.Empty<Owner>()
                group o.Id + i by o.Name + s
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                 (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (
                                ((global::System.String)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.BinaryExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    __e1.Arguments[1]
                                                ).Body
                                            ).Right
                                        )
                                    )
                                ),
                                ((global::System.Int32)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.BinaryExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    __e1.Arguments[2]
                                                ).Body
                                            ).Right
                                        )
                                    )
                                )
                            ) switch {
                                var (__c0, __c1) => global::System.Linq.Enumerable.GroupBy(
                                    global::System.Array.Empty<global::Arborist.TestFixtures.Owner>(),
                                    (o) => (o.Name + __c0),
                                    (o) => (o.Id + __c1)
                                )
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
    public void Should_handle_captured_local_in_join_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var owners = Array.Empty<Owner>();
            var value = 42;
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                from o in Array.Empty<Owner>()
                join o1 in owners on o.Id + value equals value + o1.Id
                select o.Id + value
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (
                                ((global::Arborist.TestFixtures.Owner[])
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            __e1.Arguments[1]
                                        )
                                    )
                                ),
                                ((global::System.Int32)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.BinaryExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    __e1.Arguments[2]
                                                ).Body
                                            ).Right
                                        )
                                    )
                                ),
                                ((global::System.Int32)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.BinaryExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    __e1.Arguments[3]
                                                ).Body
                                            ).Left
                                        )
                                    )
                                ),
                                ((global::System.Int32)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.BinaryExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    __e1.Arguments[4]
                                                ).Body
                                            ).Right
                                        )
                                    )
                                )
                            ) switch {
                                var (__c0, __c1, __c2, __c3) => global::System.Linq.Enumerable.Join(
                                    global::System.Array.Empty<global::Arborist.TestFixtures.Owner>(),
                                    __c0,
                                    (o) => (o.Id + __c1),
                                    (o1) => (__c2 + o1.Id),
                                    (o, o1) => (o.Id + __c3)
                                )
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
    public void Should_handle_captured_local_in_let_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                from o in Array.Empty<Owner>()
                let n = o.Name + value
                select n
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        ((global::System.Linq.Expressions.BinaryExpression)
                                            ((global::System.Linq.Expressions.NewExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    ((global::System.Linq.Expressions.MethodCallExpression)
                                                        __e1.Arguments[0]
                                                    ).Arguments[1]
                                                ).Body
                                            ).Arguments[1]
                                        ).Right
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.Linq.Enumerable.Select(
                                    global::System.Linq.Enumerable.Select(
                                        global::System.Array.Empty<global::Arborist.TestFixtures.Owner>(),
                                        (o) => new {
                                            o,
                                            n = (o.Name + __c0)
                                        }
                                    ),
                                    (__v0) => __v0.n
                                )
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
    public void Should_handle_captured_local_in_orderby_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var s = ""foo"";
            var i = 42;
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                from o in Array.Empty<Owner>()
                orderby o.Name + s, o.Id + i
                select o
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (
                                ((global::System.String)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.BinaryExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    ((global::System.Linq.Expressions.MethodCallExpression)
                                                        __e1.Arguments[0]
                                                    ).Arguments[1]
                                                ).Body
                                            ).Right
                                        )
                                    )
                                ),
                                ((global::System.Int32)
                                    global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                        ((global::System.Linq.Expressions.MemberExpression)
                                            ((global::System.Linq.Expressions.BinaryExpression)
                                                ((global::System.Linq.Expressions.LambdaExpression)
                                                    __e1.Arguments[1]
                                                ).Body
                                            ).Right
                                        )
                                    )
                                )
                            ) switch {
                                var (__c0, __c1) => global::System.Linq.Enumerable.ThenBy(
                                    global::System.Linq.Enumerable.OrderBy(
                                        global::System.Array.Empty<global::Arborist.TestFixtures.Owner>(),
                                        (o) => (o.Name + __c0)
                                    ),
                                    (o) => (o.Id + __c1)
                                )
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
    public void Should_handle_captured_local_in_quoted_select_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                from c in Array.Empty<Cat>().AsQueryable()
                select c.Name + value
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        ((global::System.Linq.Expressions.BinaryExpression)
                                            ((global::System.Linq.Expressions.LambdaExpression)
                                                ((global::System.Linq.Expressions.UnaryExpression)
                                                    __e1.Arguments[1]
                                                ).Operand
                                            ).Body
                                        ).Right
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.Linq.Queryable.Select(
                                    global::System.Linq.Queryable.AsQueryable(
                                        global::System.Array.Empty<global::Arborist.TestFixtures.Cat>()
                                    ),
                                    (c) => (c.Name + __c0)
                                )
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
    public void Should_handle_captured_local_in_where_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var value = ""foo"";
            ExpressionOnNone.Interpolate(x => x.SpliceValue(
                from o in Array.Empty<Owner>()
                where o.Name == value
                select o
            ));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Constant(
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => ((global::System.String)
                                global::Arborist.Interpolation.Internal.InterpolationInterceptorHelpers.GetCapturedLocalValue(
                                    ((global::System.Linq.Expressions.MemberExpression)
                                        ((global::System.Linq.Expressions.BinaryExpression)
                                            ((global::System.Linq.Expressions.LambdaExpression)
                                                __e1.Arguments[1]
                                            ).Body
                                        ).Right
                                    )
                                )
                            ) switch {
                                var __c0 => global::System.Linq.Enumerable.Where(
                                    global::System.Array.Empty<global::Arborist.TestFixtures.Owner>(),
                                    (o) => (o.Name == __c0)
                                )
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
