using Xunit;

namespace Arborist.CodeGen;

public partial class EvaluatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_a_constant() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(42));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    42,
                    typeof(global::System.Int32)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_constructor() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(new string('0', 3)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    new global::System.String('0', 3),
                    typeof(global::System.String)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_target_typed_constructor() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object), (x, m) => x.SpliceValue(m.InstanceMethod(new('0', 3))));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    ???.InstanceMethod(new('0', 3)),
                    typeof(global::System.Int32)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_object_initializer() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(new Cat { Name = ""Garfield"" }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    new global::Arborist.TestFixtures.Cat() {
                        Name = ""Garfield""
                    },
                    typeof(global::Arborist.TestFixtures.Cat)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_collection_initializer() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(new List<string> { ""foo"", ""bar"" }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    new global::System.Collections.Generic.List<global::System.String>() {
                        ""foo"",
                        ""bar""
                    },
                    typeof(global::System.Collections.Generic.List<global::System.String>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_dictionary_initializer() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(new Dictionary<string, int> {
                { ""foo"", 1 },
                { ""bar"", 2 }
            }));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    new global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>() {
                        { ""foo"", 1 },
                        { ""bar"", 2 }
                    },
                    typeof(global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_unary() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                new { Cat = default(Cat)! },
                x => x.SpliceValue(!x.Data.Cat.IsAlive)
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    !__data.Cat.IsAlive,
                    typeof(global::System.Boolean)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_binary() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                new { Cat = default(Cat)! },
                x => x.SpliceValue(x.Data.Cat.Name + ""foo"")
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    (__data.Cat.Name + ""foo""),
                    typeof(global::System.String)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_ternary() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                new { Cat = default(Cat)! },
                x => x.SpliceValue(x.Data.Cat.IsAlive ? ""foo"" : ""bar"")
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    (__data.Cat.IsAlive ? ""foo"" : ""bar""),
                    typeof(global::System.String)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_cast() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                new { Cat = default(Cat)! },
                x => x.SpliceValue((object)x.Data.Cat)
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    (global::System.Object)__data.Cat,
                    typeof(global::System.Object)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_instance_call() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(MemberFixture)!,
                static x => x.SpliceValue(x.Data.InstanceMethod(""foo""))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    __data.InstanceMethod(""foo""),
                    typeof(global::System.Int32)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_static_call() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                static x => x.SpliceValue(MemberFixture.StaticMethod(""foo""))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::Arborist.TestFixtures.MemberFixture.StaticMethod(""foo""),
                    typeof(global::System.Int32)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_static_call() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                static x => x.SpliceValue(MemberFixture.GenericStaticMethod(42))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::Arborist.TestFixtures.MemberFixture.GenericStaticMethod(42),
                    typeof(global::System.Int32)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_instance_call() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(MemberFixture)!,
                static x => x.SpliceValue(x.Data.GenericInstanceMethod(""foo""))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    __data.GenericInstanceMethod(""foo""),
                    typeof(global::System.String)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_instance_call_with_type_args() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(MemberFixture)!,
                static x => x.SpliceValue(x.Data.GenericInstanceMethod<IEnumerable<char>>(""foo""))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    __data.GenericInstanceMethod<global::System.Collections.Generic.IEnumerable<global::System.Char>>(""foo""),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.Char>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_static_call_with_type_args() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                static x => x.SpliceValue(MemberFixture.GenericStaticMethod<IEnumerable<char>>(""foo""))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    global::Arborist.TestFixtures.MemberFixture.GenericStaticMethod<global::System.Collections.Generic.IEnumerable<global::System.Char>>(""foo""),
                    typeof(global::System.Collections.Generic.IEnumerable<global::System.Char>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_anonymous_object_construction() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(
                default(object),
                static x => x.SpliceValue(new { foo = ""foo"", bar = 42, string.Empty })
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    new { foo = ""foo"", bar = 42, global::System.String.Empty },
                    __t0.Type
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

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
}
