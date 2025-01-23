using Microsoft.CodeAnalysis;

namespace Arborist.CodeGen;

public class InterpolationAnalysisResult(
    Location invocationLocation,
    string fileName,
    string className,
    InterpolatedTree interceptsLocationAttribute,
    InterpolatedTree interceptorMethodDeclaration,
    InterpolatedTree bodyTree,
    InterpolatedTree dataDeclaration,
    InterpolatedTree returnStatement,
    IReadOnlyList<InterpolatedValueDefinition> valueDefinitions,
    IReadOnlyList<InterpolatedTree> methodDefinitions
) : IEquatable<InterpolationAnalysisResult> {
    // N.B. Microsoft.CodeAnalysis.Location is 100% equatable, but does not implement IEquatable
    public Location InvocationLocation { get; } = invocationLocation;
    public string FileName { get; } = fileName;
    public string ClassName { get; } = className;
    public InterpolatedTree InterceptsLocationAttribute { get; } = interceptsLocationAttribute;
    public InterpolatedTree InterceptorMethodDeclaration { get; } = interceptorMethodDeclaration;
    public InterpolatedTree BodyTree { get; } = bodyTree;
    public IReadOnlyList<InterpolatedValueDefinition> ValueDefinitions { get; } = valueDefinitions;
    public IReadOnlyList<InterpolatedTree> MethodDefinitions { get; } = methodDefinitions;
    public InterpolatedTree DataDeclaration { get; } = dataDeclaration;
    public InterpolatedTree ReturnStatement { get; } = returnStatement;

    public bool IsSupported =>
        ReturnStatement.IsSupported
        && InterceptorMethodDeclaration.IsSupported
        && DataDeclaration.IsSupported
        && ValueDefinitions.All(static d => d.IsSupported)
        && MethodDefinitions.All(static d => d.IsSupported);

    public override int GetHashCode() =>
        InvocationLocation.GetHashCode();

    public bool Equals(InterpolationAnalysisResult? that) =>
        that is not null
        && this.InvocationLocation.Equals(that.InvocationLocation)
        && this.InterceptsLocationAttribute.Equals(that.InterceptsLocationAttribute)
        && this.ReturnStatement.Equals(that.ReturnStatement)
        && this.InterceptorMethodDeclaration.Equals(that.InterceptorMethodDeclaration)
        && this.DataDeclaration.Equals(that.DataDeclaration)
        && this.ValueDefinitions.SequenceEqual(that.ValueDefinitions)
        && this.MethodDefinitions.SequenceEqual(that.MethodDefinitions);
}
