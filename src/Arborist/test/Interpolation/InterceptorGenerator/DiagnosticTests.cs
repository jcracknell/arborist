using Microsoft.CodeAnalysis;
using Xunit;

namespace Arborist.Interpolation.InterceptorGenerator;

public class DiagnosticTests {
    [Fact]
    public void Should_produce_ARB001_for_closure() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var owner = new Owner();
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Owner.Id == owner.Id);
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB001_ClosureOverScopeReference,
            Severity: DiagnosticSeverity.Warning
        });
    }

    [Fact]
    public void Should_produce_ARB002_in_SpliceValue() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Owner.Id == x.SpliceValue(c.Id));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB002_EvaluatedInterpolatedParameter,
            Severity: DiagnosticSeverity.Error
        });
    }

    [Fact]
    public void Should_produce_ARB003_for_an_expression_without_splices() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate(default(object), (x, c) => c.Name == ""Garfield"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB003_NoSplices,
            Severity: DiagnosticSeverity.Warning
        });
    }
}
