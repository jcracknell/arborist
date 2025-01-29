using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Reflection;
using System.Text;

namespace Arborist.Interpolation.InterceptorGenerator;

public sealed class InterpolationInterceptorGeneratorTestBuilder {
    public static InterpolationInterceptorGeneratorTestBuilder Create(
        string @namespace = "Test"
    ) =>
        new(@namespace);

    private static readonly string AssemblyPath =
        Path.GetDirectoryName(typeof(object).Assembly.Location)!;

    private readonly List<string> _assemblies = new();
    private readonly SortedSet<string> _usings = new();
    private readonly string _namespace;
    private bool _omitEnclosingDefinitions = false;

    private InterpolationInterceptorGeneratorTestBuilder(string @namespace) {
        _namespace = @namespace;

        AddAssembly("mscorlib.dll");
        AddAssembly("System.dll");
        AddAssembly("System.Core.dll");
        AddAssembly("System.Runtime.dll");
        AddAssembly(typeof(System.Action));
        AddAssembly(typeof(System.Linq.Enumerable));
        AddAssembly(typeof(System.Linq.Expressions.Expression));
        AddAssembly(typeof(Arborist.ExpressionOnNone));

        Using("System");
        Using(typeof(System.Collections.Generic.List<>));
        Using(typeof(System.Collections.Immutable.ImmutableList<>));
        Using(typeof(System.Linq.Enumerable));
        Using(typeof(Arborist.ExpressionHelper));
        Using(typeof(Arborist.TestFixtures.Cat));
    }

    public InterpolationInterceptorGeneratorTestBuilder AddAssembly(string assemblyName) {
        _assemblies.Add(Path.Combine(AssemblyPath, assemblyName));
        return this;
    }

    public InterpolationInterceptorGeneratorTestBuilder AddAssembly(Assembly assembly) {
        _assemblies.Add(assembly.Location);
        return this;
    }

    public InterpolationInterceptorGeneratorTestBuilder AddAssembly(Type type) =>
        AddAssembly(type.Assembly);

    public InterpolationInterceptorGeneratorTestBuilder Using(string @namespace) {
        _usings.Add(@namespace);
        return this;
    }

    public InterpolationInterceptorGeneratorTestBuilder Using(Type type) {
        AddAssembly(type);
        return Using(type.Namespace!);
    }

    public InterpolationInterceptorGeneratorTestBuilder OmitEnclosingDefinitions(bool value = true) {
        _omitEnclosingDefinitions = value;
        return this;
    }

    public InterpolationInterceptorGeneratorTestResults Generate(
        string invocations
    ) {
        var inputSource = GenerateInputSource(invocations);

        var compilation = CSharpCompilation.Create("CSharpCodeGen.GenerateAssembly")
        .AddReferences(
            from assemblyPath in _assemblies.Distinct()
            select MetadataReference.CreateFromFile(assemblyPath)
        )
        .AddSyntaxTrees(CSharpSyntaxTree.ParseText(inputSource))
        .WithOptions(new CSharpCompilationOptions(
            outputKind: OutputKind.ConsoleApplication,
            generalDiagnosticOption: ReportDiagnostic.Info
        ));

        var analysisResults = new List<InterpolationAnalysisResult>();
        var generator = new InterpolationInterceptorGenerator {
            AnalysisResultHandler = (result) => {
                analysisResults.Add(result);
            }
        };

        var driver = CSharpGeneratorDriver.Create(generator)
        .WithUpdatedAnalyzerConfigOptions(new TestAnalyzerConfigOptionsProvider(new() {
            ["build_property._ArboristInterceptorsNamespaces"] = "Arborist.Interpolation.Interceptors"
        }))
        .RunGeneratorsAndUpdateCompilation(compilation, out _, out var generatorDiagnostics);

        return new InterpolationInterceptorGeneratorTestResults(
            compilation: compilation,
            analysisResults: analysisResults,
            generatedTrees: driver.GetRunResult().GeneratedTrees,
            diagnostics: [
                ..compilation.GetDiagnostics(),
                ..generatorDiagnostics
            ]
        );
    }

    private string GenerateInputSource(string invocations) {
        var sb = new StringBuilder();
        foreach(var usingNamespace in _usings)
            sb.AppendLine($"using {usingNamespace};");

        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine($"");
        
        if(_omitEnclosingDefinitions) {
            sb.AppendLine(invocations);
        } else {
            sb.AppendLine($"public static class Program {{");
            sb.AppendLine($"    public static void Main() {{");
            sb.AppendLine($"         {invocations}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");
        }

        return sb.ToString();
    }

    private sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> entries)
        : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options = new TestAnalyzerConfigOptions(entries);

        public override AnalyzerConfigOptions GlobalOptions =>
            _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            _options;
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> entries) : AnalyzerConfigOptions {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
            entries.TryGetValue(key, out value);
    }
}
