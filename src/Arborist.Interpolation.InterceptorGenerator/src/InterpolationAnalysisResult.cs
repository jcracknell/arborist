using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

public class InterpolationAnalysisResult(
    string assemblyName,
    string sourceFilePath,
    Location invocationLocation,
    bool interceptionRequired,
    InterpolatedTree interceptsLocationAttribute,
    InterpolatedTree interceptorMethodDeclaration,
    InterpolatedTree bodyTree,
    InterpolatedTree? dataDeclaration,
    InterpolatedTree returnStatement,
    IReadOnlyList<InterpolatedValueDefinition> valueDefinitions,
    IReadOnlyList<InterpolatedTree> methodDefinitions
) : IEquatable<InterpolationAnalysisResult> {
    public string AssemblyName { get; } = assemblyName;
    public string SourceFilePath { get; } = sourceFilePath;
    // N.B. Microsoft.CodeAnalysis.Location is 100% equatable, but does not implement IEquatable
    public Location InvocationLocation { get; } = invocationLocation;
    public bool InterceptionRequired { get; } = interceptionRequired;
    public InterpolatedTree InterceptsLocationAttribute { get; } = interceptsLocationAttribute;
    public InterpolatedTree InterceptorMethodDeclaration { get; } = interceptorMethodDeclaration;
    public InterpolatedTree BodyTree { get; } = bodyTree;
    public IReadOnlyList<InterpolatedValueDefinition> ValueDefinitions { get; } = valueDefinitions;
    public IReadOnlyList<InterpolatedTree> MethodDefinitions { get; } = methodDefinitions;
    public InterpolatedTree? DataDeclaration { get; } = dataDeclaration;
    public InterpolatedTree ReturnStatement { get; } = returnStatement;

    public bool IsSupported =>
        ReturnStatement.IsSupported
        && InterceptorMethodDeclaration.IsSupported
        && DataDeclaration?.IsSupported is not false
        && ValueDefinitions.All(static d => d.IsSupported)
        && MethodDefinitions.All(static d => d.IsSupported);

    public override int GetHashCode() =>
        InvocationLocation.GetHashCode();

    public bool Equals(InterpolationAnalysisResult? that) =>
        that is not null
        && this.InvocationLocation.Equals(that.InvocationLocation)
        && this.SourceFilePath.Equals(that.SourceFilePath)
        && this.AssemblyName.Equals(that.AssemblyName)
        && this.InterceptsLocationAttribute.Equals(that.InterceptsLocationAttribute)
        && this.ReturnStatement.Equals(that.ReturnStatement)
        && this.InterceptorMethodDeclaration.Equals(that.InterceptorMethodDeclaration)
        && (this.DataDeclaration?.Equals(that.DataDeclaration!) ?? that.DataDeclaration is null)
        && this.ValueDefinitions.SequenceEqual(that.ValueDefinitions)
        && this.MethodDefinitions.SequenceEqual(that.MethodDefinitions);
}
