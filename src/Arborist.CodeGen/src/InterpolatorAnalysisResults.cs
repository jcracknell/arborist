using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Arborist.CodeGen;

public class InterpolatorAnalysisResults(
    InterpolatorInvocationContext invocationContext,
    IReadOnlyList<InterpolatedTree> parameterTrees,
    InterpolatedTree bodyTree
) {
    public InterpolatedTreeBuilder Builder { get; } = invocationContext.Builder;
    public InvocationExpressionSyntax Invocation { get; } = invocationContext.InvocationSyntax;
    public IMethodSymbol MethodSymbol { get; } = invocationContext.MethodSymbol;
    public IParameterSymbol? DataParameter { get; } = invocationContext.DataParameter;
    public IParameterSymbol ExpressionParameter { get; } = invocationContext.ExpressionParameter;
    public IReadOnlyList<InterpolatedTree> ParameterTrees { get; } = parameterTrees;
    public InterpolatedTree BodyTree { get; } = bodyTree;

    public bool IsSupported =>
        BodyTree.IsSupported
        && Builder.ValueDefinitions.All(static d => d.Initializer.IsSupported)
        && Builder.MethodDefinitions.All(static d => d.IsSupported);

    public string InvocationId { get; } = ComputeInvocationId(invocationContext.InvocationSyntax);

    private static string ComputeInvocationId(InvocationExpressionSyntax node) {
        var lineSpan = node.GetLocation().GetLineSpan();
        var filePath = node.SyntaxTree.FilePath;
        var bufferSize = Math.Max(64, Encoding.UTF8.GetByteCount(filePath));
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try {
            using var hash = System.Security.Cryptography.SHA256.Create();

            // Source checksum
            if(node.SyntaxTree.TryGetText(out var sourceText)) {
                var checkSum = sourceText.GetChecksum();
                checkSum.CopyTo(buffer);
                hash.TransformBlock(buffer, 0, checkSum.Length, default, 0);
            }

            // Call position
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0), lineSpan.StartLinePosition.Line);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4), lineSpan.StartLinePosition.Character);
            hash.TransformBlock(buffer, 0, 8, default, 0);

            // UTF-8 encoded file path
            var pathBytes = Encoding.UTF8.GetBytes(filePath, 0, filePath.Length, buffer, 0);
            hash.TransformBlock(buffer, 0, pathBytes, default, 0);

            hash.TransformFinalBlock(buffer, 0, 0);

            return hash.Hash.Take(8).MkString(static b => b.ToString("x2"), "");
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
