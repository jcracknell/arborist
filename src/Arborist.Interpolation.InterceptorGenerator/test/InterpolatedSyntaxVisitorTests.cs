using Xunit;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_constant() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => ""foo"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant(
                    ""foo"",
                    typeof(global::System.String)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_constructor() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => new string('0', 3));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.New(
                    typeof(global::System.String).GetConstructor(new global::System.Type[] {
                        typeof(global::System.Char),
                        typeof(global::System.Int32)
                    })!,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant('0', typeof(global::System.Char)),
                        global::System.Linq.Expressions.Expression.Constant(3, typeof(global::System.Int32))
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_target_typed_constructor() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object), (x, m) => m.InstanceMethod(new('0', 3)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __p0,
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.New(
                            typeof(global::System.String).GetConstructor(new global::System.Type[] {
                                typeof(global::System.Char),
                                typeof(global::System.Int32)
                            })!,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant('0', typeof(global::System.Char)),
                                global::System.Linq.Expressions.Expression.Constant(3, typeof(global::System.Int32))
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
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => new Cat { Name = ""Garfield"" });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MemberInit(
                    global::System.Linq.Expressions.Expression.New(
                        typeof(global::Arborist.TestFixtures.Cat).GetConstructor(global::System.Type.EmptyTypes)!,
                        new global::System.Linq.Expressions.Expression[] { }
                    ),
                    new global::System.Linq.Expressions.MemberBinding[] {
                        global::System.Linq.Expressions.Expression.Bind(
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!,
                            global::System.Linq.Expressions.Expression.Constant(
                                ""Garfield"",
                                typeof(global::System.String)
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
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => new List<string> { ""foo"", ""bar"" });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.ListInit(
                    global::System.Linq.Expressions.Expression.New(
                        typeof(global::System.Collections.Generic.List<global::System.String>).GetConstructor(global::System.Type.EmptyTypes)!,
                        new global::System.Linq.Expressions.Expression[] { }
                    ),
                    new global::System.Linq.Expressions.ElementInit[] {
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(
                                    ""foo"",
                                    typeof(global::System.String)
                                )
                            }
                        ),
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(
                                    ""bar"",
                                    typeof(global::System.String)
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
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => new Dictionary<string, int> {
                { ""foo"", 1 }
                { ""bar"", 2 }
            });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.ListInit(
                    global::System.Linq.Expressions.Expression.New(
                        typeof(global::System.Collections.Generic.Dictionary<global::System.String, global::System.Int32>)
                        .GetConstructor(global::System.Type.EmptyTypes)!,
                        new global::System.Linq.Expressions.Expression[] { }
                    ),
                    new global::System.Linq.Expressions.ElementInit[] {
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(""foo"", typeof(global::System.String)),
                                global::System.Linq.Expressions.Expression.Constant(1, typeof(global::System.Int32))
                            }
                        ),
                        global::System.Linq.Expressions.Expression.ElementInit(
                            __m0,
                            new global::System.Linq.Expressions.Expression[] {
                                global::System.Linq.Expressions.Expression.Constant(""bar"", typeof(global::System.String)),
                                global::System.Linq.Expressions.Expression.Constant(2, typeof(global::System.Int32))
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
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object), (x, f) => f.InstanceField);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Field(
                    __p0,
                    typeof(global::Arborist.TestFixtures.MemberFixture).GetField(""InstanceField"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_instance_property() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object), (x, f) => f.InstanceProperty);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Property(
                    __p0,
                    typeof(global::Arborist.TestFixtures.MemberFixture).GetProperty(""InstanceProperty"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );

    }

    [Fact]
    public void Should_handle_static_field() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => MemberFixture.StaticField);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Field(
                    default(global::System.Linq.Expressions.Expression),
                    typeof(global::Arborist.TestFixtures.MemberFixture).GetField(""StaticField"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_static_property() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => MemberFixture.StaticProperty);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Property(
                    default(global::System.Linq.Expressions.Expression),
                    typeof(global::Arborist.TestFixtures.MemberFixture).GetProperty(""StaticProperty"")!
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_instance_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object), (x, f) => f.InstanceMethod(""foo""));
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
                            typeof(global::System.String)
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_instance_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object), (x, f) => f.GenericInstanceMethod(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => default(global::Arborist.TestFixtures.MemberFixture)!.GenericInstanceMethod(
                        default(global::System.String)!
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
                            typeof(global::System.String)
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_instance_method_with_type_params() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<MemberFixture>.Interpolate(default(object), (x, f) => f.GenericInstanceMethod<IEnumerable<char>>(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => default(global::Arborist.TestFixtures.MemberFixture)!.GenericInstanceMethod<global::System.Collections.Generic.IEnumerable<global::System.Char>>(
                        default(global::System.Collections.Generic.IEnumerable<global::System.Char>)!
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
                            typeof(global::System.String)
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_static_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => MemberFixture.StaticMethod(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Constant(
                            ""foo"",
                            typeof(global::System.String)
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_static_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => MemberFixture.GenericStaticMethod(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => global::Arborist.TestFixtures.MemberFixture.GenericStaticMethod(
                        default(global::System.String)!
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
                            typeof(global::System.String)
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_generic_static_method_with_type_params() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => global::Arborist.TestFixtures.MemberFixture.GenericStaticMethod<IEnumerable<char>>(""foo""));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var m0Definition = results.AnalysisResults[0].ValueDefinitions.SingleOrDefault(d => d.Identifier == "__m0");
        Assert.NotNull(m0Definition);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.ExpressionOnNone.GetMethodInfo(
                    () => global::Arborist.TestFixtures.MemberFixture.GenericStaticMethod<global::System.Collections.Generic.IEnumerable<global::System.Char>>(
                        default(global::System.Collections.Generic.IEnumerable<global::System.Char>)!
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
                            typeof(global::System.String)
                        )
                    }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_unary() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => !c.IsAlive);
        ");


        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeUnary(
                    global::System.Linq.Expressions.ExpressionType.Not,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""IsAlive"")!
                    ),
                    typeof(global::System.Boolean),
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_cast() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => (decimal)42);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Convert(
                    global::System.Linq.Expressions.Expression.Constant(
                        42,
                        typeof(global::System.Int32)
                    ),
                    typeof(global::System.Decimal)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_binary() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Name + ""foo"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.Add,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        ""foo"",
                        typeof(global::System.String)
                    ),
                    false,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_ternary() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.IsAlive ? c.Name : ""(Deceased)"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Condition(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""IsAlive"")!
                    ),
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        ""(Deceased)"",
                        typeof(global::System.String)
                    ),
                    typeof(global::System.String)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_lambda() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) => o.Cats.Any(c => c.IsAlive));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Call(
                    __m0,
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Cats"")!
                        ),
                        global::System.Linq.Expressions.Expression.Lambda(
                            global::System.Linq.Expressions.Expression.Property(
                                __p1,
                                typeof(global::Arborist.TestFixtures.Cat).GetProperty(""IsAlive"")!
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
    public void Should_handle_default_value_type() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Id == default);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.Equal,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        default(global::System.Int32),
                        typeof(global::System.Int32)
                    ),
                    false,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_default_reference_type() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Name == default);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.Equal,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        default(global::System.String)!,
                        typeof(global::System.String)
                    ),
                    false,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_anonymous_type() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => new { foo = c.Name, bar = c.Age });
        ");

        Assert.Equal(1, results.AnalysisResults.Count);

        var analysisResult = results.AnalysisResults[0];
        var anonymousTypeDefinition = analysisResult.ValueDefinitions.Single(d => d.Identifier == "__t0");

        CodeGenAssert.CodeEqual(
            expected: @"
                global::Arborist.Interpolation.Internal.TypeRef.Create(new {
                    foo = default(global::System.String)!,
                    bar = default(global::System.Nullable<global::System.Int32>)
                })
            ",
            actual: anonymousTypeDefinition.Initializer.ToString()
        );

        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.New(
                    __t0.Type.GetConstructors()[0],
                    new global::System.Linq.Expressions.Expression[] {
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Name"")!
                        ),
                        global::System.Linq.Expressions.Expression.Property(
                            __p0,
                            typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Age"")!
                        )
                    },
                    new global::System.Reflection.MemberInfo[] {
                        __t0.Type.GetProperty(""foo"")!,
                        __t0.Type.GetProperty(""bar"")!
                    }
                )
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
                global::System.Linq.Expressions.Expression.Call(
                    global::System.Linq.Expressions.Expression.Constant(
                        __data,
                        typeof(global::System.Object)
                    ),
                    __m0,
                    new global::System.Linq.Expressions.Expression[] { }
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
