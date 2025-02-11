using System.Buffers;
using System.Text;

namespace Arborist.Interpolation.InterceptorGenerator;

/// <summary>
/// Groups <see cref="InterpolationAnalysisResult"/> instances by source file in preparation
/// for output. This is a development-centric affordance, as it makes it substantially easier
/// to find the generated interceptor associated with an invocation.
/// </summary>
public sealed class InterpolationAnalysisGroup : IEquatable<InterpolationAnalysisGroup> {
    public static ImmutableArray<InterpolationAnalysisGroup> CreateGroups(
        ImmutableArray<InterpolationAnalysisResult> analyses,
        CancellationToken cancellationToken
    ) {
        var groups = new Dictionary<(string, string), List<InterpolationAnalysisResult>>();
        foreach(var analysis in analyses) {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Group invocations by source file and assembly, as the assembly name is used in
            // the generated file name - I mean you never know? Maybe it's possible for a
            // compilation to span assemblies?
            var key = (analysis.SourceFilePath, analysis.AssemblyName);
            if(!groups.TryGetValue(key, out var group)) {
                group = new List<InterpolationAnalysisResult>(1);
                groups.Add(key, group);
            }
                
            group.Add(analysis);
        }
        
        var builder = ImmutableArray.CreateBuilder<InterpolationAnalysisGroup>(groups.Count);
        foreach(var entry in groups) {
            cancellationToken.ThrowIfCancellationRequested();
            
            var group = entry.Value;
            builder.Add(CreateGroup(group[0], group));
        }
        
        return builder.ToImmutable();
    }

    private static InterpolationAnalysisGroup CreateGroup(
        InterpolationAnalysisResult exemplar,
        IReadOnlyList<InterpolationAnalysisResult> analyses
    ) {
        var groupId = CreateGroupId(exemplar);
        var className = $"InterpolationInterceptors{groupId}";
        var fileBaseName = $"{exemplar.AssemblyName.Replace("_", "__").Replace('.', '_')}_{className}";
        
        return new InterpolationAnalysisGroup(
            assemblyName: exemplar.AssemblyName,
            sourceFilePath: exemplar.SourceFilePath,
            generatedSourceName: $"{InterpolationInterceptorGenerator.INTERCEPTOR_NAMESPACE}.{fileBaseName}.g.cs",
            className: className,
            analyses:
                analyses.Select(static a => new { Analysis = a, StartLine = a.InvocationLocation.GetLineSpan().StartLinePosition })
                .OrderBy(static x => (x.StartLine.Line, x.StartLine.Character))
                .Select(static x => x.Analysis)
                .ToList()
        );
    }
    
    private static string CreateGroupId(InterpolationAnalysisResult exemplar) {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try {
            using var hash = System.Security.Cryptography.SHA256.Create();
            
            hash.TransformString(exemplar.SourceFilePath, Encoding.Unicode, buffer);
            hash.TransformString(exemplar.AssemblyName, Encoding.Unicode, buffer);
            hash.TransformFinalBlock(buffer, 0, 0);
            
            return hash.Hash.Take(8).MkString(static b => b.ToString("x2"), "");
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    
    private InterpolationAnalysisGroup(
        string assemblyName,
        string sourceFilePath,
        string generatedSourceName,
        string className,
        IReadOnlyList<InterpolationAnalysisResult> analyses
    ) {
        AssemblyName = assemblyName;
        SourceFilePath = sourceFilePath;
        GeneratedSourceName = generatedSourceName;
        ClassName = className;
        Analyses = analyses;
    }

    public string AssemblyName { get; }
    public string SourceFilePath { get; }
    public string GeneratedSourceName { get; }
    public string ClassName { get; }
    public IReadOnlyList<InterpolationAnalysisResult> Analyses { get; }

    public override int GetHashCode() =>
        HashCode.Combine(SourceFilePath, AssemblyName);

    public override bool Equals(object? obj) =>
        Equals(obj as InterpolationAnalysisGroup);

    public bool Equals(InterpolationAnalysisGroup? that) =>
        that is not null 
        && this.SourceFilePath.Equals(that.SourceFilePath)
        && this.AssemblyName.Equals(that.AssemblyName)
        && this.GeneratedSourceName.Equals(that.GeneratedSourceName)
        && this.ClassName.Equals(that.ClassName)
        && this.Analyses.SequenceEqual(that.Analyses);
}
