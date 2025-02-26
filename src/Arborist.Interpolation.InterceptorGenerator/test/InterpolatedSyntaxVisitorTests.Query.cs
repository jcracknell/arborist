namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_select_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var data = new {
                OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
                CatName = ExpressionOn<Cat>.Of(c => c.Name)
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in x.SpliceBody(o, x.Data.OwnerCats)
                select x.SpliceBody(c, x.Data.CatName)
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => (global::System.Linq.Expressions.MemberExpression)(__e1.Arguments[1]) switch {
                                var __e2 => __data.OwnerCats
                            } switch {
                                var __v0 => global::Arborist.ExpressionHelper.Replace(
                                    __v0.Body,
                                    global::Arborist.Internal.Collections.SmallDictionary.Create(
                                        new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                            __v0.Parameters[0],
                                            __e1.Arguments[0]
                                        )
                                    )
                                )
                            }
                        },
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[1]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.CatName
                                    } switch {
                                        var __v1 => global::Arborist.ExpressionHelper.Replace(
                                            __v1.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v1.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                __e1.Parameters
                            )
                        }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_quoted_select_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x =>
                from c in Array.Empty<Cat>().AsQueryable()
                select x.SpliceValue(""foo"")
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        __e0.Arguments[0],
                        (global::System.Linq.Expressions.UnaryExpression)(__e0.Arguments[1]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Quote(
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Operand) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e2.Body) switch {
                                            var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                    var __e4 => ""foo""
                                                },
                                                __e3.Type
                                            )
                                        },
                                        __e2.Parameters
                                    )
                                }
                            )
                        }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_quoted_from_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var data = new {
                CatOwnerCats = ExpressionOn<Cat>.Of(c => c.Owner.CatsQueryable),
                ObjectHashCode = ExpressionOn<object>.Of(o => o.GetHashCode())
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in o.CatsQueryable
                from d in x.SpliceBody(c, x.Data.CatOwnerCats)
                select c.Name + d.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        __e0.Arguments[0],
                        (global::System.Linq.Expressions.UnaryExpression)(__e0.Arguments[1]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Quote(
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Operand) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e2.Body) switch {
                                            var __e3 => (global::System.Linq.Expressions.MemberExpression)(__e3.Arguments[1]) switch {
                                                var __e4 => __data.CatOwnerCats
                                            } switch {
                                                var __v0 => global::Arborist.ExpressionHelper.Replace(
                                                    __v0.Body,
                                                    global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                        new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                            __v0.Parameters[0],
                                                            __e3.Arguments[0]
                                                        )
                                                    )
                                                )
                                            }
                                        },
                                        __e2.Parameters
                                    )
                                }
                            )
                        },
                        __e0.Arguments[2]
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_cast_in_initial_from_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var data = new {
                OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
                ObjectHashCode = ExpressionOn<object>.Of(o => o.GetHashCode())
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from object c in x.SpliceBody(o, x.Data.OwnerCats)
                select x.SpliceBody(c, x.Data.ObjectHashCode)
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Call(
                                default(global::System.Linq.Expressions.Expression),
                                __e1.Method,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.OwnerCats
                                    } switch {
                                        var __v0 => global::Arborist.ExpressionHelper.Replace(
                                            __v0.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v0.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                }
                            )
                        },
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[1]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.ObjectHashCode
                                    } switch {
                                        var __v1 => global::Arborist.ExpressionHelper.Replace(
                                            __v1.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v1.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                __e1.Parameters
                            )
                        }
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
            var data = new {
                OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
                CatId = ExpressionOn<Cat>.Of(c => c.Id)
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in o.Cats
                join c1 in x.SpliceBody(o, x.Data.OwnerCats) on x.SpliceBody(c, x.Data.CatId) equals x.SpliceValue(42)
                select c1.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        __e0.Arguments[0],
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[1]) switch {
                            var __e1 => (global::System.Linq.Expressions.MemberExpression)(__e1.Arguments[1]) switch {
                                var __e2 => __data.OwnerCats
                            } switch {
                                var __v0 => global::Arborist.ExpressionHelper.Replace(
                                    __v0.Body,
                                    global::Arborist.Internal.Collections.SmallDictionary.Create(
                                        new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                            __v0.Parameters[0],
                                            __e1.Arguments[0]
                                        )
                                    )
                                )
                            }
                        },
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[2]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.CatId
                                    } switch {
                                        var __v1 => global::Arborist.ExpressionHelper.Replace(
                                            __v1.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v1.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                __e1.Parameters
                            )
                        },
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[3]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                        (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                            var __e3 => 42
                                        },
                                        __e2.Type
                                    )
                                },
                                __e1.Parameters
                            )
                        },
                        __e0.Arguments[4]
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_quoted_join_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var data = new {
                OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
                CatId = ExpressionOn<Cat>.Of(c => c.Id)
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in o.CatsQueryable
                join c1 in x.SpliceBody(o, x.Data.OwnerCats) on x.SpliceBody(c, x.Data.CatId) equals x.SpliceValue(42)
                select c1.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        __e0.Arguments[0],
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[1]) switch {
                            var __e1 => (global::System.Linq.Expressions.MemberExpression)(__e1.Arguments[1]) switch {
                                var __e2 => __data.OwnerCats
                            } switch {
                                var __v0 => global::Arborist.ExpressionHelper.Replace(
                                    __v0.Body,
                                    global::Arborist.Internal.Collections.SmallDictionary.Create(
                                        new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                            __v0.Parameters[0],
                                            __e1.Arguments[0]
                                        )
                                    )
                                )
                            }
                        },
                        (global::System.Linq.Expressions.UnaryExpression)(__e0.Arguments[2]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Quote(
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Operand) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e2.Body) switch {
                                            var __e3 => (global::System.Linq.Expressions.MemberExpression)(__e3.Arguments[1]) switch {
                                                var __e4 => __data.CatId
                                            } switch {
                                                var __v1 => global::Arborist.ExpressionHelper.Replace(
                                                    __v1.Body,
                                                    global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                        new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                            __v1.Parameters[0],
                                                            __e3.Arguments[0]
                                                        )
                                                    )
                                                )
                                            }
                                        },
                                        __e2.Parameters
                                    )
                                }
                            )
                        },
                        (global::System.Linq.Expressions.UnaryExpression)(__e0.Arguments[3]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Quote(
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Operand) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e2.Body) switch {
                                            var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                    var __e4 => 42
                                                },
                                                __e3.Type
                                            )
                                        },
                                        __e2.Parameters
                                    )
                                }
                            )
                        },
                        __e0.Arguments[4]
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
            var data = new {
                OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
                CatId = ExpressionOn<Cat>.Of(c => c.Id)
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in o.Cats
                join object c1 in x.SpliceBody(o, x.Data.OwnerCats) on x.SpliceBody(c, x.Data.CatId) equals x.SpliceValue(42)
                select c1
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        __e0.Arguments[0],
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[1]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Call(
                                default(global::System.Linq.Expressions.Expression),
                                __e1.Method,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.OwnerCats
                                    } switch {
                                        var __v0 => global::Arborist.ExpressionHelper.Replace(
                                            __v0.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v0.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                }
                            )
                        },
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[2]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.CatId
                                    } switch {
                                        var __v1 => global::Arborist.ExpressionHelper.Replace(
                                            __v1.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v1.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                __e1.Parameters
                            )
                        },
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[3]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                        (global::System.Linq.Expressions.ConstantExpression)(__e2.Arguments[0]) switch {
                                            var __e3 => 42
                                        },
                                        __e2.Type
                                    )
                                },
                                __e1.Parameters
                            )
                        },
                        __e0.Arguments[4]
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
            var data = new {
                OwnerCats = ExpressionOn<Owner>.Of(o => o.Cats),
                CatId = ExpressionOn<Cat>.Of(c => c.Id)
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in o.Cats
                join c1 in x.SpliceBody(o, x.Data.OwnerCats) on x.SpliceBody(c, x.Data.CatId) equals x.SpliceValue(42)
                into cs
                from cc in cs
                select cc.Age
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Call(
                                default(global::System.Linq.Expressions.Expression),
                                __e1.Method,
                                __e1.Arguments[0],
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[1]) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.OwnerCats
                                    } switch {
                                        var __v0 => global::Arborist.ExpressionHelper.Replace(
                                            __v0.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v0.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Arguments[2]) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e2.Body) switch {
                                            var __e3 => (global::System.Linq.Expressions.MemberExpression)(__e3.Arguments[1]) switch {
                                                var __e4 => __data.CatId
                                            } switch {
                                                var __v1 => global::Arborist.ExpressionHelper.Replace(
                                                    __v1.Body,
                                                    global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                        new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                            __v1.Parameters[0],
                                                            __e3.Arguments[0]
                                                        )
                                                    )
                                                )
                                            }
                                        },
                                        __e2.Parameters
                                    )
                                },
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Arguments[3]) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.MethodCallExpression)(__e2.Body) switch {
                                            var __e3 => global::System.Linq.Expressions.Expression.Constant(
                                                (global::System.Linq.Expressions.ConstantExpression)(__e3.Arguments[0]) switch {
                                                    var __e4 => 42
                                                },
                                                __e3.Type
                                            )
                                        },
                                        __e2.Parameters
                                    )
                                },
                                __e1.Arguments[4]
                            )
                        },
                        __e0.Arguments[1],
                        __e0.Arguments[2]
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_group_by_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var data = new {
                NameSelector = ExpressionOn<Cat>.Of(c => c.Name),
                AgeSelector = ExpressionOn<Cat>.Of(c => c.Age)
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in o.Cats
                group x.SpliceBody(c, x.Data.NameSelector) by x.SpliceBody(c, x.Data.AgeSelector)
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        __e0.Arguments[0],
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[1]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.AgeSelector
                                    } switch {
                                        var __v0 => global::Arborist.ExpressionHelper.Replace(
                                            __v0.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v0.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                __e1.Parameters
                            )
                        },
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[2]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.NameSelector
                                    } switch {
                                        var __v1 => global::Arborist.ExpressionHelper.Replace(
                                            __v1.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v1.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                __e1.Parameters
                            )
                        }
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_group_by_clause_with_identity_element_selector() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var data = new {
                AgeSelector = ExpressionOn<Cat>.Of(c => c.Age)
            };

            ExpressionOn<Owner>.Interpolate(data, (x, o) =>
                from c in o.Cats
                group c by x.SpliceBody(c, x.Data.AgeSelector)
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        __e0.Arguments[0],
                        (global::System.Linq.Expressions.LambdaExpression)(__e0.Arguments[1]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Lambda(
                                __e1.Type,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Body) switch {
                                    var __e2 => (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[1]) switch {
                                        var __e3 => __data.AgeSelector
                                    } switch {
                                        var __v0 => global::Arborist.ExpressionHelper.Replace(
                                            __v0.Body,
                                            global::Arborist.Internal.Collections.SmallDictionary.Create(
                                                new global::System.Collections.Generic.KeyValuePair<global::System.Linq.Expressions.Expression, global::System.Linq.Expressions.Expression>(
                                                    __v0.Parameters[0],
                                                    __e2.Arguments[0]
                                                )
                                            )
                                        )
                                    }
                                },
                                __e1.Parameters
                            )
                        }
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
            ExpressionOn<Owner>.Interpolate(""Garfield"", (x, o) =>
                from c in o.Cats
                let n = x.SpliceValue(x.Data)
                select c.Name + n
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Call(
                                default(global::System.Linq.Expressions.Expression),
                                __e1.Method,
                                __e1.Arguments[0],
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Arguments[1]) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.NewExpression)(__e2.Body) switch {
                                            var __e3 => global::System.Linq.Expressions.Expression.New(
                                                __e3.Constructor!,
                                                new global::System.Linq.Expressions.Expression[] {
                                                    __e3.Arguments[0],
                                                    (global::System.Linq.Expressions.MethodCallExpression)(__e3.Arguments[1]) switch {
                                                        var __e4 => global::System.Linq.Expressions.Expression.Constant(
                                                            (global::System.Linq.Expressions.MemberExpression)(__e4.Arguments[0]) switch {
                                                                var __e5 => __data
                                                            },
                                                            __e4.Type
                                                        )
                                                    }
                                                },
                                                __e3.Members
                                            )
                                        },
                                        __e2.Parameters
                                    )
                                }
                            )
                        },
                        __e0.Arguments[1]
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_handle_orderby_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate((x, o) =>
                from c in o.Cats
                orderby x.SpliceValue(42) ascending, c.Age descending
                select c.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Call(
                                default(global::System.Linq.Expressions.Expression),
                                __e1.Method,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Call(
                                        default(global::System.Linq.Expressions.Expression),
                                        __e2.Method,
                                        __e2.Arguments[0],
                                        (global::System.Linq.Expressions.LambdaExpression)(__e2.Arguments[1]) switch {
                                            var __e3 => global::System.Linq.Expressions.Expression.Lambda(
                                                __e3.Type,
                                                (global::System.Linq.Expressions.MethodCallExpression)(__e3.Body) switch {
                                                    var __e4 => global::System.Linq.Expressions.Expression.Constant(
                                                        (global::System.Linq.Expressions.ConstantExpression)(__e4.Arguments[0]) switch {
                                                            var __e5 => 42
                                                        },
                                                        __e4.Type
                                                    )
                                                },
                                                __e3.Parameters
                                            )
                                        }
                                    )
                                },
                                __e1.Arguments[1]
                            )
                        },
                        __e0.Arguments[1]
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
            ExpressionOn<Owner>.Interpolate(ExpressionOn<string>.Identity, (x, o) =>
                from c in o.Cats
                where c.Name == x.SpliceValue(""foo"")
                select c.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Call(
                                default(global::System.Linq.Expressions.Expression),
                                __e1.Method,
                                __e1.Arguments[0],
                                (global::System.Linq.Expressions.LambdaExpression)(__e1.Arguments[1]) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Lambda(
                                        __e2.Type,
                                        (global::System.Linq.Expressions.BinaryExpression)(__e2.Body) switch {
                                            var __e3 => global::System.Linq.Expressions.Expression.MakeBinary(
                                                __e3.NodeType,
                                                __e3.Left,
                                                (global::System.Linq.Expressions.MethodCallExpression)(__e3.Right) switch {
                                                    var __e4 => global::System.Linq.Expressions.Expression.Constant(
                                                        (global::System.Linq.Expressions.ConstantExpression)(__e4.Arguments[0]) switch {
                                                            var __e5 => ""foo""
                                                        },
                                                        __e4.Type
                                                    )
                                                },
                                                __e3.IsLiftedToNull,
                                                __e3.Method
                                            )
                                        },
                                        __e2.Parameters
                                    )
                                }
                            )
                        },
                        __e0.Arguments[1]
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_query_continuation() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(Array.Empty<Cat>(), (x, o) =>
                from c in x.SpliceValue(x.Data)
                select c.Name
                into n
                select n.Length
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                (global::System.Linq.Expressions.MethodCallExpression)(expression.Body) switch {
                    var __e0 => global::System.Linq.Expressions.Expression.Call(
                        default(global::System.Linq.Expressions.Expression),
                        __e0.Method,
                        (global::System.Linq.Expressions.MethodCallExpression)(__e0.Arguments[0]) switch {
                            var __e1 => global::System.Linq.Expressions.Expression.Call(
                                default(global::System.Linq.Expressions.Expression),
                                __e1.Method,
                                (global::System.Linq.Expressions.MethodCallExpression)(__e1.Arguments[0]) switch {
                                    var __e2 => global::System.Linq.Expressions.Expression.Constant(
                                        (global::System.Linq.Expressions.MemberExpression)(__e2.Arguments[0]) switch {
                                            var __e3 => __data
                                        },
                                        __e2.Type
                                    )
                                },
                                __e1.Arguments[1]
                            )
                        },
                        __e0.Arguments[1]
                    )
                }
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
