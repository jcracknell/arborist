using Xunit;

namespace Arborist.Interpolation.InterceptorGenerator;

public partial class EvaluatedSyntaxVisitorTests {
    [Fact]
    public void Should_work_for_as() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Cat)!, x => x.SpliceValue(x.Data as IFormattable));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant( 
                    (__data as global::System.IFormattable),
                    typeof(global::System.IFormattable)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }

    [Fact]
    public void Should_work_for_is() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(Cat)!, x => x.SpliceValue(x.Data is IFormattable));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        CodeGenAssert.CodeEqual(
            expected: @"
                global::System.Linq.Expressions.Expression.Constant( 
                    (__data is global::System.IFormattable),
                    typeof(global::System.Boolean)
                )
            ",
            actual: results.AnalysisResults[0].BodyTree.ToString()
        );
    }
}
