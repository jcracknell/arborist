using Microsoft.CodeAnalysis;

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
        Assert.False(results.AnalysisResults[0].BodyTree.IsModified);
        Assert.True(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB003_NoSplices,
            Severity: DiagnosticSeverity.Warning
        });
    }

    [Fact]
    public void Should_produce_ARB004_for_evaluated_private_field_access() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private int _privateField;

                public void Main() {
                    ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(_privateField));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB004_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB004_for_evaluated_private_method_call() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private int PrivateMethod() => throw new NotImplementedException();

                public void Main() {
                    ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(PrivateMethod()));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB004_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB004_for_evaluated_private_static_method_call() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private static int PrivateMethod() => throw new NotImplementedException();

                public void Main() {
                    ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(PrivateMethod()));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB004_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB004_for_evaluated_private_property_access() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private int PrivateProperty => throw new NotImplementedException();

                public void Main() {
                    ExpressionOnNone.Interpolate(default(object), x => x.SpliceValue(PrivateProperty));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB004_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }


    [Fact]
    public void Should_produce_ARB005_for_expression_referencing_type_param_from_callsite_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Foo {
                public void Bar<A>() {
                    ExpressionOnNone.Interpolate(default(object), x => Array.Empty<A>().Contains(x.SpliceValue((A)x.Data)));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB005_ReferencesCallSiteTypeParameter,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB005_for_expression_referencing_type_param_from_callsite_class() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Foo<A> {
                public void Bar() {
                    ExpressionOnNone.Interpolate(default(object), x => Array.Empty<A>().Contains(x.SpliceValue((A)x.Data)));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB005_ReferencesCallSiteTypeParameter,
            Severity: DiagnosticSeverity.Info
        });
    }
}
