using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Text;

namespace Arborist.CodeGen;

public sealed class InterpolatorInterceptorGeneratorTestBuilder {
    public static InterpolatorInterceptorGeneratorTestBuilder Create(
        string @namespace = "Test"
    ) =>
        new(@namespace);

    private static readonly string AssemblyPath =
        Path.GetDirectoryName(typeof(object).Assembly.Location)!;

    private readonly List<string> _assemblies = new();
    private readonly SortedSet<string> _usings = new();
    private readonly string _namespace;

    private InterpolatorInterceptorGeneratorTestBuilder(string @namespace) {
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
        Using(typeof(Arborist.CodeGen.Fixtures.Cat));
    }

    public InterpolatorInterceptorGeneratorTestBuilder AddAssembly(string assemblyName) {
        _assemblies.Add(Path.Combine(AssemblyPath, assemblyName));
        return this;
    }

    public InterpolatorInterceptorGeneratorTestBuilder AddAssembly(Assembly assembly) {
        _assemblies.Add(assembly.Location);
        return this;
    }

    public InterpolatorInterceptorGeneratorTestBuilder AddAssembly(Type type) =>
        AddAssembly(type.Assembly);

    public InterpolatorInterceptorGeneratorTestBuilder Using(string @namespace) {
        _usings.Add(@namespace);
        return this;
    }

    public InterpolatorInterceptorGeneratorTestBuilder Using(Type type) {
        AddAssembly(type);
        return Using(type.Namespace!);
    }

    public InterpolatorInterceptorGeneratorTestResults Generate(
        string invocations
    ) {
        var inputSource = GenerateInputSource(invocations);

        var compilation = CSharpCompilation.Create("CSharpCodeGen.GenerateAssembly")
        .AddReferences(
            from assemblyPath in _assemblies.Distinct()
            select MetadataReference.CreateFromFile(assemblyPath)
        )
        .AddSyntaxTrees(CSharpSyntaxTree.ParseText(inputSource))
        .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var analysisResults = new List<InterpolatorAnalysisResults>();
        var generator = new InterpolatorInterceptorGenerator {
            AnalysisResultHandler = (result) => {
                analysisResults.Add(result);
            }
        };

        var driver = CSharpGeneratorDriver.Create(generator)
        .RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return new InterpolatorInterceptorGeneratorTestResults(
            compilation: compilation,
            analysisResults: analysisResults,
            generatedTrees: driver.GetRunResult().GeneratedTrees
        );
    }

    private string GenerateInputSource(string invocations) {
        var sb = new StringBuilder();
        foreach(var usingNamespace in _usings)
            sb.AppendLine($"using {usingNamespace};");

        sb.AppendLine($"");
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine($"");
        sb.AppendLine($"public static class Program {{");
        sb.AppendLine($"    public static void Main() {{");
        sb.AppendLine($"         {invocations}");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        return sb.ToString();
    }
}