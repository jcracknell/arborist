using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Arborist.Analyzers;

public static class InterpolationDiagnosticDescriptors {
    public static DiagnosticDescriptor ARB999_UnsupportedSyntax { get; } =
        Create(
            severity: DiagnosticSeverity.Info,
            title: "Unsupported syntax",
            message: [$"Encountered syntax which is unsupported by {typeof(InterpolationAnalyzer).FullName}."]
        );

    public static DiagnosticDescriptor ARB001_NoSplices { get; } =
        Create(
            severity: DiagnosticSeverity.Warning,
            title: "Interpolated expression contains no splices",
            message: ["The interpolated expression contains no splices, and will have no effect."]
        );

    public static DiagnosticDescriptor ARB002_InterpolationContextReference { get; } = Create(
        severity: DiagnosticSeverity.Error,
        title: "Illegal interpolation context reference",
        message: [
            "The interpolated expression contains a reference to the interpolation context which is not part of",
            "an interpolated splicing call or evaluated data access expression."
        ]
    );

    public static DiagnosticDescriptor ARB003_InterpolatedParameterReference { get; } = Create(
        severity: DiagnosticSeverity.Error,
        title: "Interpolated parameter reference in evaluated splice argument",
        message: [
            "The interpolated expression contains a reference to one of its parameters inside of an evaluated",
            "splice argument."
        ]
    );

    public static DiagnosticDescriptor ARB004_NestedInterpolation { get; } = Create(
        severity: DiagnosticSeverity.Warning,
        title: "Interpolated expression contains a call to an expression interpolation method",
        message: [
            "The interpolated expression contains a call to an expression interpolation method which will be",
            "present in the resulting expression tree."
        ]
    );

    private static DiagnosticDescriptor Create(
        DiagnosticSeverity severity,
        string title,
        string[] message,
        [CallerMemberName] string? memberName = default
    ) =>
        new DiagnosticDescriptor(
            id: GetDiagnosticId(memberName),
            title: title,
            messageFormat: string.Join(" ", message),
            category: "Arborist.Interpolation",
            defaultSeverity: severity,
            isEnabledByDefault: true
        );

    private static string GetDiagnosticId(string? memberName) {
        if(memberName is null)
            throw new ArgumentNullException(nameof(memberName));
        if(Regex.Match(memberName, @"^(ARB[0-9]+)_") is not { Success: true } match)
            throw new ArgumentException("Diagnostic member name did not have the expected form.");

        return match.Groups[1].Value;
    }
}
