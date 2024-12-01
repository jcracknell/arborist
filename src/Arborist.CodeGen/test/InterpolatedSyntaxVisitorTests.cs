using Xunit;

namespace Arborist.CodeGen;

public class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_constant() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => ""foo"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    ""foo"",
                    __t0.Type
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_constructor() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new string('0', 3));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.New(
                    __t0.Type.GetConstructor(new global::System.Type[] {
                        __t1.Type,
                        __t2.Type
                    })!,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant('0', __t1.Type),
                        global::System.Linq.Expressions.Expression.Constant(3, __t2.Type)
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_target_typed_constructor() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate((x, m) => m.InstanceMethod(new('0', 3)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __p0,
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.New(
                            __t2.Type.GetConstructor(new global::System.Type[] {
                                __t3.Type,
                                __t4.Type
                            })!,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant('0', __t3.Type),
                                global::System.Linq.Expressions.Expression.Constant(3, __t4.Type)
                            }
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_object_initializer() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new Cat { Name = ""Garfield"" });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MemberInit(
                    global::System.Linq.Expressions.Expression.New(
                        __t0.Type.GetConstructor(global::System.Type.EmptyTypes)!,
                        new global::System.Linq.Expressions.Expression[] { }
                    ),
                    new global::System.Linq.Expressions.MemberBinding[] {
                        global::System.Linq.Expressions.Expression.Bind(
                            __t0.Type.GetMember(""Name"")!,
                            global::System.Linq.Expressions.Expression.Constant(
                                ""Garfield"",
                                __t1.Type
                            )
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_collection_initializer() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new List<string> { ""foo"", ""bar"" });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.ListInit(
                    global::System.Linq.Expressions.Expression.New(
                        __t0.Type.GetConstructor(global::System.Type.EmptyTypes)!,
                        new global::System.Linq.Expressions.Expression[] { }
                    ),
                    new global::System.Linq.Expressions.ElementInit[] {
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(
                                    ""foo"",
                                    __t1.Type
                                )
                            }
                        ),
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(
                                    ""bar"",
                                    __t1.Type
                                )
                            }
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_dictionary_initializer() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => new Dictionary<string, int> {
                { ""foo"", 1 }
                { ""bar"", 2 }
            });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.ListInit(
                    global::System.Linq.Expressions.Expression.New(
                        __t0.Type.GetConstructor(global::System.Type.EmptyTypes)!,
                        new global::System.Linq.Expressions.Expression[] { }
                    ),
                    new global::System.Linq.Expressions.ElementInit[] {
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(""foo"", __t3.Type),
                                global::System.Linq.Expressions.Expression.Constant(1, __t2.Type)
                            }
                        ),
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(""bar"", __t3.Type),
                                global::System.Linq.Expressions.Expression.Constant(2, __t2.Type)
                            }
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );

    }

    [Fact]
    public void Should_handle_instance_field() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate((x, f) => f.InstanceField);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Field(
                    __p0,
                    __t0.Type.GetField(""InstanceField"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_instance_property() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate((x, f) => f.InstanceProperty);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Property(
                    __p0,
                    __t0.Type.GetProperty(""InstanceProperty"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );

    }

    [Fact]
    public void Should_handle_static_field() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => MemberFixture.StaticField);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Field(
                    __t0.Default,
                    __t1.Type.GetField(""StaticField"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_static_property() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => MemberFixture.StaticProperty);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Property(
                    __t0.Default,
                    __t1.Type.GetProperty(""StaticProperty"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_instance_method() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate((x, f) => f.InstanceMethod(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __p0,
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant(
                            ""foo"",
                            __t2.Type
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_instance_method() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate((x, f) => f.GenericInstanceMethod(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].Builder.ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => __t0.Default.GenericInstanceMethod(__t1.Default)
                )
            ",
            actual: m0Definition.Initializer.ToString()
        );

        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __p0,
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant(
                            ""foo"",
                            __t1.Type
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_instance_method_with_type_params() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate((x, f) => f.GenericInstanceMethod<IEnumerable<char>>(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].Builder.ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => __t0.Default.GenericInstanceMethod<global::System.Collections.Generic.IEnumerable<global::System.Char>>(
                        __t1.Default
                    )
                )
            ",
            actual: m0Definition.Initializer.ToString()
        );

        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __p0,
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant(
                            ""foo"",
                            __t2.Type
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_static_method() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => MemberFixture.StaticMethod(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant(
                            ""foo"",
                            __t2.Type
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_static_method() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => MemberFixture.GenericStaticMethod(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].Builder.ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => global::Arborist.CodeGen.Fixtures.MemberFixture.GenericStaticMethod(__t0.Default)
                )
            ",
            actual: m0Definition.Initializer.ToString()
        );

        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant(
                            ""foo"",
                            __t0.Type
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_static_method_with_type_params() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => global::Arborist.CodeGen.Fixtures.MemberFixture.GenericStaticMethod<IEnumerable<char>>(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].Builder.ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => global::Arborist.CodeGen.Fixtures.MemberFixture.GenericStaticMethod<global::System.Collections.Generic.IEnumerable<global::System.Char>>(
                        __t0.Default
                    )
                )
            ",
            actual: m0Definition.Initializer.ToString()
        );

        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant(
                            ""foo"",
                            __t1.Type
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_unary() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => !c.IsAlive);
        ");


        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeUnary(
                    global::System.Linq.Expressions.ExpressionType.Not,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        __t0.Type.GetProperty(""IsAlive"")!
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_binary() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.Name + ""foo"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.Add,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        __t0.Type.GetProperty(""Name"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        ""foo"",
                        __t1.Type
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_ternary() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.IsAlive ? c.Name : ""(Deceased)"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Condition(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        __t0.Type.GetProperty(""IsAlive"")!
                    ),
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        __t0.Type.GetProperty(""Name"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        ""(Deceased)"",
                        __t1.Type
                    ),
                    __t1.Type
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_lambda() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate((x, o) => o.Cats.Any(c => c.IsAlive));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            __t0.Type.GetProperty(""Cats"")!
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                __p1,
                                __t3.Type.GetProperty(""IsAlive"")!
                            ),
                            __p1
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_anonymous_type() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => new { foo = c.Name, bar = c.Age });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var analysisResult = results.AnalysisResults[0];
        var anonymousTypeDefinition = analysisResult.Builder.ValueDefinitions.Single(d => d.Identifier == "__t1");

        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.Interpolation.Internal.TypeRef.Create(new {
                    foo = __t2.Default,
                    bar = __t3.Default
                })
            ",
            actual: anonymousTypeDefinition.Initializer.ToString()
        );

        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.New(
                    __t1.Type.GetConstructors()[0],
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        __t0.Type.GetProperty(""Name"")!
                    ),
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        __t0.Type.GetProperty(""Age"")!
                    )
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
