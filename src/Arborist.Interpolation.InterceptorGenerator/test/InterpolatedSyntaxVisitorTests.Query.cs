using Xunit;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_select_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                select c.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p1,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                        ),
                        __p1
                    )
                )   
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_cast_in_initial_from_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from object c in o.Cats
                select c.GetHashCode()
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m2,
                    global::System.Linq.Expressions.Expression.Call(
                        __m0,
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Call(
                            __p1,
                            __m1,
                            new global::System.Linq.Expressions.Expression[] { }
                        ),
                        __p1
                    )
                )   
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );

    }

    [Fact]
    public void Should_handle_join_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                join c1 in o.Cats on c.Id equals c1.Id
                select c1.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                    ),
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p1,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                        ),
                        __p1
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p2,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                        ),
                        __p2
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p2,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                        ),
                        __p1,
                        __p2
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_join_clause_with_cast() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                join object c1 in o.Cats on c.Id equals c1.GetHashCode()
                select c1.GetHashCode()
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m2,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                    ),
                    global::System.Linq.Expressions.Expression.Call(
                        __m0,
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p1,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                        ),
                        __p1
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Call(
                            __p2,
                            __m1,
                            new global::System.Linq.Expressions.Expression[] { }
                        ),
                        __p2
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Call(
                            __p2,
                            __m1,
                            new global::System.Linq.Expressions.Expression[] { }
                        ),
                        __p1,
                        __p2
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_join_into_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                join c1 in o.Cats on c.Id equals c1.Id into cs
                from cc in cs
                select cc.Age
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m1,
                    global::System.Linq.Expressions.Expression.Call(
                        __m0,
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        ),
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                __p1,
                                typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                            ),
                            __p1
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                __p2,
                                typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                            ),
                            __p2
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.New(
                                __t0.Type.GetConstructors()[0],
                                new global::System.Linq.Expressions.Expression[] {
                                    __p1,
                                    __p3
                                },
                                new global::System.Reflection.MemberInfo[] {
                                    __t0.Type.GetProperty(""c"")!,
                                    __t0.Type.GetProperty(""cs"")!
                                }
                            ),
                            __p1,
                            __p3
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p4,
                            __t0.Type.GetProperty(""cs"")!
                        ),
                        __p4
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p5,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Age"")!
                        ),
                        __p4,
                        __p5
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_group_by_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                group c by c.Age
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(
                            ""Cats""
                        )!
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p1,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(
                                ""Age""
                            )!
                        ),
                        __p1
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        __p1,
                        __p1
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_let_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                let n = o.Name
                select c.Name + n
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m2,
                    global::System.Linq.Expressions.Expression.Call(
                        __m0,
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.New(
                                __t0.Type.GetConstructors()[0],
                                new global::System.Linq.Expressions.Expression[] {
                                    __p1,
                                    global::System.Linq.Expressions.Expression.Property(
                                        __p0,
                                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Name"")!
                                    )
                                },
                                new global::System.Reflection.MemberInfo[] {
                                    __t0.Type.GetProperty(""c"")!,
                                    __t0.Type.GetProperty(""n"")!
                                }
                            ),
                            __p1
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.MakeBinary(
                            global::System.Linq.Expressions.ExpressionType.Add,
                            global::System.Linq.Expressions.Expression.Property(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p2,
                                    __t0.Type.GetProperty(""c"")!
                                ),
                                typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                            ),
                            global::System.Linq.Expressions.Expression.Property(
                                __p2,
                                __t0.Type.GetProperty(""n"")!
                            ),
                            false,
                            __m1
                        ),
                        __p2
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_handle_orderby_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                orderby c.Name ascending, c.Age descending
                select c.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m2,
                    global::System.Linq.Expressions.Expression.Call(
                        __m1,
                        global::System.Linq.Expressions.Expression.Call(
                            __m0,
                            global::System.Linq.Expressions.Expression.Property(
                                __p0,
                                typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                            ),
                            global::System.Linq.Expressions.Expression.Lambda(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p1,
                                    typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                                ),
                                __p1
                            )
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                __p1,
                                typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Age"")!
                            ),
                            __p1
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p1,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                        ),
                        __p1
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_where_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                where c.Age == 8
                select c.Name
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m2,
                    global::System.Linq.Expressions.Expression.Call(
                        __m1,
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.MakeBinary(
                                global::System.Linq.Expressions.ExpressionType.Equal,
                                global::System.Linq.Expressions.Expression.Property(
                                    __p1,
                                    typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Age"")!
                                ),
                                global::System.Linq.Expressions.Expression.Convert(
                                    global::System.Linq.Expressions.Expression.Constant(
                                        8,
                                        typeof(global::System.Int32)
                                    ),
                                    typeof(global::System.Nullable<global::System.Int32>)
                                ),
                                false,
                                __m0
                            ),
                            __p1
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p1,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                        ),
                        __p1
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_transparent_identifiers_in_from_clauses() {
        // Here the third from clause introduces a transparent identifier of the form
        // new { * = new { a, b }, c }, however we need a fourth clause to force it to appear
        // in the projection expression provided to SelectMany
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from a in o.Cats
                from b in o.Cats
                from c in o.Cats
                from d in o.Cats
                select b
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m2,
                    global::System.Linq.Expressions.Expression.Call(
                        __m1,
                        global::System.Linq.Expressions.Expression.Call(
                            __m0,
                            global::System.Linq.Expressions.Expression.Property(
                                __p0,
                                typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                            ),
                            global::System.Linq.Expressions.Expression.Lambda(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p0,
                                    typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                                ),
                                __p1
                            ),
                            global::System.Linq.Expressions.Expression.Lambda(
                                global::System.Linq.Expressions.Expression.New(
                                    __t0.Type.GetConstructors()[0],
                                    new global::System.Linq.Expressions.Expression[] {
                                        __p1,
                                        __p2
                                    },
                                    new global::System.Reflection.MemberInfo[] {
                                        __t0.Type.GetProperty(""a"")!,
                                        __t0.Type.GetProperty(""b"")!
                                    }
                                ),
                                __p1,
                                __p2
                            )
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                __p0,
                                typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                            ),
                            __p3
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.New(
                                __t3.Type.GetConstructors()[0],
                                new global::System.Linq.Expressions.Expression[] {
                                    __p3,
                                    __p4
                                },
                                new global::System.Reflection.MemberInfo[] {
                                    __t3.Type.GetProperty(""__v0"")!,
                                    __t3.Type.GetProperty(""c"")!
                                }
                            ),
                            __p3,
                            __p4
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        ),
                        __p5
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            global::System.Linq.Expressions.Expression.Property(
                                __p5,
                                __t3.Type.GetProperty(""__v0"")!
                            ),
                            __t0.Type.GetProperty(""b"")!
                        ),
                        __p5,
                        __p6
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_transparent_identifiers_in_let_clauses() {
        // Here the second let clause introduces a transparent identifier of the form
        // new { * = new { c, n }, m }
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from c in o.Cats
                let n = o.Name
                let m = o.Id
                select c.Name + n + m
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m4,
                    global::System.Linq.Expressions.Expression.Call(
                        __m1,
                        global::System.Linq.Expressions.Expression.Call(
                            __m0,
                            global::System.Linq.Expressions.Expression.Property(
                                __p0,
                                typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                            ),
                            global::System.Linq.Expressions.Expression.Lambda(
                                global::System.Linq.Expressions.Expression.New(
                                    __t0.Type.GetConstructors()[0],
                                    new global::System.Linq.Expressions.Expression[] {
                                        __p1,
                                        global::System.Linq.Expressions.Expression.Property(
                                            __p0,
                                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Name"")!
                                        )
                                    },
                                    new global::System.Reflection.MemberInfo[] {
                                        __t0.Type.GetProperty(""c"")!,
                                        __t0.Type.GetProperty(""n"")!
                                    }
                                ),
                                __p1
                            )
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.New(
                                __t3.Type.GetConstructors()[0],
                                new global::System.Linq.Expressions.Expression[] {
                                    __p2,
                                    global::System.Linq.Expressions.Expression.Property(
                                        __p0,
                                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Id"")!
                                    )
                                },
                                new global::System.Reflection.MemberInfo[] {
                                    __t3.Type.GetProperty(""__v0"")!,
                                    __t3.Type.GetProperty(""m"")!
                                }
                            ),
                            __p2
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.MakeBinary(
                            global::System.Linq.Expressions.ExpressionType.Add,
                            global::System.Linq.Expressions.Expression.MakeBinary(
                                global::System.Linq.Expressions.ExpressionType.Add,
                                global::System.Linq.Expressions.Expression.Property(
                                    global::System.Linq.Expressions.Expression.Property(
                                        global::System.Linq.Expressions.Expression.Property(
                                            __p3,
                                            __t3.Type.GetProperty(""__v0"")!
                                        ),
                                        __t0.Type.GetProperty(""c"")!
                                    ),
                                    typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                                ),
                                global::System.Linq.Expressions.Expression.Property(
                                    global::System.Linq.Expressions.Expression.Property(
                                        __p3,
                                        __t3.Type.GetProperty(""__v0"")!
                                    ),
                                    __t0.Type.GetProperty(""n"")!
                                ),
                                false,
                                __m2
                            ),
                            global::System.Linq.Expressions.Expression.Convert(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p3,
                                    __t3.Type.GetProperty(""m"")!
                                ),
                                typeof(global::System.Object)
                            ),
                            false,
                            __m3
                        ),
                        __p3
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_transparent_identifiers_in_join_clause() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) =>
                from a in o.Cats
                join b in o.Cats on a.Id equals b.Id
                join c in o.Cats on a.Id equals c.Id
                join d in o.Cats on a.Id equals d.Id
                select a
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m2,
                    global::System.Linq.Expressions.Expression.Call(
                        __m1,
                        global::System.Linq.Expressions.Expression.Call(
                            __m0,
                            global::System.Linq.Expressions.Expression.Property(
                                __p0,
                                typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                            ),
                            global::System.Linq.Expressions.Expression.Property(
                                __p0,
                                typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                            ),
                            global::System.Linq.Expressions.Expression.Lambda(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p1,
                                    typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                                ),
                                __p1
                            ),
                            global::System.Linq.Expressions.Expression.Lambda(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p2,
                                    typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                                ),
                                __p2
                            ),
                            global::System.Linq.Expressions.Expression.Lambda(
                                global::System.Linq.Expressions.Expression.New(
                                    __t0.Type.GetConstructors()[0],
                                    new global::System.Linq.Expressions.Expression[] {
                                        __p1,
                                        __p2
                                    },
                                    new global::System.Reflection.MemberInfo[] {
                                        __t0.Type.GetProperty(""a"")!,
                                        __t0.Type.GetProperty(""b"")!
                                    }
                                ),
                                __p1,
                                __p2
                            )
                        ),
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p3,
                                    __t0.Type.GetProperty(""a"")!
                                ),
                                typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                            ),
                            __p3
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                __p4,
                                typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                            ),
                            __p4
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.New(
                                __t3.Type.GetConstructors()[0],
                                new global::System.Linq.Expressions.Expression[] {
                                    __p3,
                                    __p4
                                },
                                new global::System.Reflection.MemberInfo[] {
                                    __t3.Type.GetProperty(""__v0"")!,
                                    __t3.Type.GetProperty(""c"")!
                                }
                            ),
                            __p3,
                            __p4
                        )
                    ),
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            global::System.Linq.Expressions.Expression.Property(
                                global::System.Linq.Expressions.Expression.Property(
                                    __p5,
                                    __t3.Type.GetProperty(""__v0"")!
                                ),
                                __t0.Type.GetProperty(""a"")!
                            ),
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                        ),
                        __p5
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            __p6,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                        ),
                        __p6
                    ),
                    global::System.Linq.Expressions.Expression.Lambda(
                        global::System.Linq.Expressions.Expression.Property(
                            global::System.Linq.Expressions.Expression.Property(
                                __p5,
                                __t3.Type.GetProperty(""__v0"")!
                            ),
                            __t0.Type.GetProperty(""a"")!
                        ),
                        __p5,
                        __p6
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
