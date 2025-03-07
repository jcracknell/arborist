using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

public class DiagnosticTests {
    [Fact]
    public void Should_produce_ARB001_for_context_reference_in_interpolated() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(x => x == x.SpliceConstant(default(object)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB001_InterpolationContextReference,
            Severity: DiagnosticSeverity.Error
        });
    }

    [Fact]
    public void Should_produce_ARB001_for_data_reference_in_interpolated() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object), x => x.Data == x.SpliceConstant(default(object)));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB001_InterpolationContextReference,
            Severity: DiagnosticSeverity.Error
        });
    }

    [Fact]
    public void Should_not_produce_ARB001_for_shadowed_context_identifier() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOnNone.Interpolate(default(object),
                x => Array.Empty<string>().SingleOrDefault(x => x == null) == x.SpliceConstant(""foo"")
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.True(results.AnalysisResults[0].IsSupported);
        Assert.DoesNotContain(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB001_InterpolationContextReference
        });
    }

    [Fact]
    public void Should_produce_ARB003_for_evaluating_interpolated_identifier() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.Owner.Id == x.SpliceConstant(c.Id));
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB003_EvaluatedInterpolatedParameter,
            Severity: DiagnosticSeverity.Error
        });
    }

    [Fact]
    public void Should_produce_ARB003_for_evaluating_interpolated_identifier_introduced_in_lambda() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Owner>.Interpolate((x, o) =>
                o.Cats.Any(c => c.Name == x.SpliceConstant(c.Name))
            );
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB003_EvaluatedInterpolatedParameter,
            Severity: DiagnosticSeverity.Error
        });
    }

    [Fact]
    public void Should_produce_ARB004_for_an_expression_without_splices() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            ExpressionOn<Cat>.Interpolate((x, c) => c.Name == ""Garfield"");
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.True(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB004_NoSplices,
            Severity: DiagnosticSeverity.Warning
        });
    }

    [Fact]
    public void Should_produce_ARB005_for_evaluated_private_field_access() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private int _privateField;

                public void Main() {
                    ExpressionOnNone.Interpolate(x => x.SpliceConstant(_privateField));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB005_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB005_for_evaluated_private_method_call() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private int PrivateMethod() => throw new NotImplementedException();

                public void Main() {
                    ExpressionOnNone.Interpolate(x => x.SpliceConstant(PrivateMethod()));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB005_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB005_for_evaluated_private_static_method_call() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private static int PrivateMethod() => throw new NotImplementedException();

                public void Main() {
                    ExpressionOnNone.Interpolate(x => x.SpliceConstant(PrivateMethod()));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB005_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB005_for_evaluated_private_property_access() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Test {
                private int PrivateProperty => throw new NotImplementedException();

                public void Main() {
                    ExpressionOnNone.Interpolate(x => x.SpliceConstant(PrivateProperty));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.False(results.AnalysisResults[0].IsSupported);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB005_InaccessibleSymbolReference,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB006_for_expression_referencing_type_param_from_callsite_method() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Foo {
                public void Bar<A>() {
                    ExpressionOnNone.Interpolate(default(object), x => Array.Empty<A>().Contains(x.SpliceConstant((A)x.Data)));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB006_ReferencesCallSiteTypeParameter,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB006_for_expression_referencing_type_param_from_callsite_class() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .OmitEnclosingDefinitions()
        .Generate(@"
            public class Foo<A> {
                public void Bar() {
                    ExpressionOnNone.Interpolate(default(object), x => Array.Empty<A>().Contains(x.SpliceConstant((A)x.Data)));
                }
            }
        ");

        Assert.Equal(1, results.AnalysisResults.Count);
        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB006_ReferencesCallSiteTypeParameter,
            Severity: DiagnosticSeverity.Info
        });
    }

    [Fact]
    public void Should_produce_ARB007_for_non_literal_expression() {
        var results = InterpolationInterceptorGeneratorTestBuilder.Create()
        .Generate(@"
            var expression = ExpressionOn<IInterpolationContext>.Of(x => 42);
            ExpressionOnNone.Interpolate(expression);
        ");

        Assert.Contains(results.Diagnostics, diagnostic => diagnostic is {
            Id: InterpolationDiagnostics.ARB007_NonLiteralInterpolatedExpression,
            Severity: DiagnosticSeverity.Info
        });
    }
}
