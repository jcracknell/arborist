using Xunit;

namespace Arborist.CodeGen;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_work_for_i32_add() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) => o.Id + 42);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.Add,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Id"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        42,
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
    public void Should_work_for_i32_add_lifted() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate(default(object), (x, o) => new int?(42) + null);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.Add,
                    global::System.Linq.Expressions.Expression.New(
                        typeof(global::System.Nullable<global::System.Int32>).GetConstructor(
                            new global::System.Type[] {
                                typeof(global::System.Int32)
                            }
                        )!,
                        new global::System.Linq.Expressions.Expression[] {
                            global::System.Linq.Expressions.Expression.Constant(
                                42,
                                typeof(global::System.Int32)
                            )
                        }
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        default(global::System.Nullable<global::System.Int32>),
                        typeof(global::System.Nullable<global::System.Int32>)
                    ),
                    true,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_i32_lt() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Id < 42)
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.LessThan,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Id"")!
                    ),
                    global::System.Linq.Expressions.Expression.Constant(
                        42,
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
    public void Should_work_for_i32_lt_lifted() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Age < 42)
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.MakeBinary(
                    global::System.Linq.Expressions.ExpressionType.LessThan,
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Age"")!
                    ),
                    global::System.Linq.Expressions.Expression.Convert(
                        global::System.Linq.Expressions.Expression.Constant(
                            42,
                            typeof(global::System.Int32)
                        ),
                        typeof(global::System.Nullable<global::System.Int32>)
                    ),
                    false,
                    __m0
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_string_add() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Name + ""bar"");
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
                        ""bar"",
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
    public void Should_work_for_as() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Owner as IFormattable);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.TypeAs(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Owner"")!
                    ),
                    typeof(global::System.IFormattable)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_is() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Owner is IFormattable);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.TypeIs(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Cat).GetProperty(""Owner"")!
                    ),
                    typeof(global::System.IFormattable)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
