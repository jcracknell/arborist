using Xunit;

namespace Arborist.CodeGen;

public partial class InterpolatedSyntaxVisitorTests {
    [Fact]
    public void Should_handle_implicit_boxing_conversion() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate<object?, object>(default(object), (x, o) => 42);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Convert(
                    global::System.Linq.Expressions.Expression.Constant(
                        42,
                        typeof(global::System.Int32)
                    ),
                    typeof(global::System.Object)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_implicit_numeric_conversion() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate<object?, decimal>(default(object), (x, o) => o.Id);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Convert(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Id"")!
                    ),
                    typeof(global::System.Decimal)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_handle_implicit_user_defined_conversion() {
        var results = InterpolatorInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate<object?, ImplicitlyConvertible<string>>(default(object), (x, o) => o.Name);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Convert(
                    global::System.Linq.Expressions.Expression.Property(
                        __p0,
                        typeof(global::Arborist.TestFixtures.Owner).GetProperty(""Name"")!
                    ),
                    typeof(global::Arborist.TestFixtures.ImplicitlyConvertible<global::System.String>)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
