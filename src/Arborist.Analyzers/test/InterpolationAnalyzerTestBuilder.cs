using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.Reflection;
using System.Text;

namespace Arborist.Analyzers;

public sealed class InterpolationAnalyzerTestBuilder {
    public static InterpolationAnalyzerTestBuilder Create(
        string @namespace = "Test"
    ) =>
        new(@namespace);

    private static readonly string AssemblyPath =
        Path.GetDirectoryName(typeof(object).Assembly.Location)!;

    private readonly List<string> _assemblies = new();
    private readonly SortedSet<string> _usings = new();
    private readonly string _namespace;
    private bool _omitEnclosingDefinitions = false;

    private InterpolationAnalyzerTestBuilder(string @namespace) {
        _namespace = @namespace;

        AddAssembly("mscorlib.dll");
        AddAssembly("System.dll");
        AddAssembly("System.Core.dll");
        AddAssembly("System.Runtime.dll");
        // There's a weird thing going on here where type List<> purports to be in System.Private.CoreLib,
        // but requires assembly System.Collections to be loaded, and I don't want to deal with figuring
        // it out at the moment.
        AddAssembly("System.Collections.dll");

        Using(typeof(System.Action));
        Using(typeof(System.Collections.Generic.List<>));
        Using(typeof(System.Collections.Immutable.ImmutableList<>));
        Using(typeof(System.Linq.Enumerable));
        Using(typeof(System.Linq.Queryable));
        Using(typeof(System.Linq.Expressions.Expression));
        Using(typeof(Arborist.ExpressionHelper));
        Using(typeof(Arborist.Interpolation.IInterpolationContext));
        Using(typeof(Arborist.TestFixtures.Cat));
    }

    public InterpolationAnalyzerTestBuilder AddAssembly(string assemblyName) {
        _assemblies.Add(Path.Combine(AssemblyPath, assemblyName));
        return this;
    }

    public InterpolationAnalyzerTestBuilder AddAssembly(Assembly assembly) {
        _assemblies.Add(assembly.Location);
        return this;
    }

    public InterpolationAnalyzerTestBuilder AddAssembly(Type type) =>
        AddAssembly(type.Assembly);

    public InterpolationAnalyzerTestBuilder Using(string @namespace) {
        _usings.Add(@namespace);
        return this;
    }

    public InterpolationAnalyzerTestBuilder Using(Type type) {
        AddAssembly(type);
        return Using(type.Namespace!);
    }

    public InterpolationAnalyzerTestBuilder OmitEnclosingDefinitions(bool value = true) {
        _omitEnclosingDefinitions = value;
        return this;
    }

    public async Task Generate(string invocations) {
        var test = new CSharpAnalyzerTest<InterpolationAnalyzer, DefaultVerifier>();
        test.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.Sources.Add(GenerateInputSource(invocations));

        foreach(var assembly in _assemblies)
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(assembly));

        await test.RunAsync();
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
}
