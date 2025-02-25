using Microsoft.CodeAnalysis;

namespace Arborist.Interpolation.InterceptorGenerator;

internal static partial class SymbolHelpers {
    public enum TypeNameFailureReason {
        AnonymousType,
        Inaccessible,
        TypeParameter,
        Unhandled
    }

    public readonly struct TypeNameResult {
        public static TypeNameResult Success => default;

        public static TypeNameResult Failure(TypeNameFailureReason reason, ITypeSymbol typeSymbol) =>
            new(reason, typeSymbol);

        private readonly TypeNameFailureReason _reason;
        private readonly ITypeSymbol _typeSymbol;

        private TypeNameResult(TypeNameFailureReason reason, ITypeSymbol typeSymbol) {
            _reason = reason;
            _typeSymbol = typeSymbol;
        }

        public bool IsFailure => _typeSymbol is not null;
        public bool IsSuccess => _typeSymbol is null;

        /// <exception cref="InvalidOperationException">
        /// Thrown if the result was not a failure.
        /// </exception>
        public TypeNameFailureReason Reason {
            get {
                if(_typeSymbol is null)
                    throw new InvalidOperationException(nameof(Reason));

                return _reason;
            }
        }

        /// <exception cref="InvalidOperationException">
        /// Thrown if the result was not a failure.
        /// </exception>
        public ITypeSymbol TypeSymbol {
            get {
                if(_typeSymbol is null)
                    throw new InvalidOperationException(nameof(TypeSymbol));

                return _typeSymbol;
            }
        }
    }

    /// <summary>
    /// Attempts to render the name of the provided <paramref name="typeSymbol"/>, returning a
    /// <see cref="TypeNameResult"/> indicating whether or not the type was nameable, and the reason
    /// for failure if it was not.
    /// </summary>
    public static TypeNameResult TryGetTypeName(
        ITypeSymbol typeSymbol,
        IReadOnlyDictionary<ITypeParameterSymbol, string> typeParameterMappings,
        out string typeName
    ) {
        using var writer = PooledStringWriter.Rent();

        var result = TryWriteTypeName(writer, typeSymbol, typeParameterMappings)    ;
        typeName = writer.ToString();

        return result;
    }

    private static TypeNameResult TryWriteTypeName(
        PooledStringWriter writer,
        ITypeSymbol typeSymbol,
        IReadOnlyDictionary<ITypeParameterSymbol, string> typeParameterMappings
    ) {
        switch(typeSymbol) {
            case { IsAnonymousType: true }:
              return TypeNameResult.Failure(TypeNameFailureReason.AnonymousType, typeSymbol);

            case INamedTypeSymbol named:
                if(!IsAccessible(named))
                    return TypeNameResult.Failure(TypeNameFailureReason.Inaccessible, typeSymbol);

                var containingTypeResult = TryWriteContainingTypeName(writer, named, typeParameterMappings);
                if(containingTypeResult.IsFailure)
                    return containingTypeResult;

                writer.Write(named.Name);

                if(named.IsGenericType) {
                    writer.Write('<');
                    for(var i = 0; i < named.TypeArguments.Length; i++) {
                        if(i != 0)
                            writer.Write(", ");

                        var argumentResult = TryWriteTypeName(writer, named.TypeArguments[i], typeParameterMappings);
                        if(argumentResult.IsFailure)
                            return argumentResult;
                    }
                    writer.Write('>');
                }
                break;


            case IArrayTypeSymbol array:
                var elementTypeResult = TryWriteTypeName(writer, array.ElementType, typeParameterMappings);
                if(elementTypeResult.IsFailure)
                    return elementTypeResult;

                // TODO: multi-dimensional arrays
                writer.Write("[]");
                break;

            case ITypeParameterSymbol typeParameter:
                if(!typeParameterMappings.TryGetValue((ITypeParameterSymbol)typeParameter.WithNullableAnnotation(NullableAnnotation.None), out var mappedTypeParameter))
                    return TypeNameResult.Failure(TypeNameFailureReason.TypeParameter, typeSymbol);

                writer.Write(mappedTypeParameter);
                break;

            case IDynamicTypeSymbol:
                writer.Write("dynamic");
                break;

            default:
                return TypeNameResult.Failure(TypeNameFailureReason.Unhandled, typeSymbol);
        }

        if(typeSymbol is { NullableAnnotation: NullableAnnotation.Annotated, IsValueType: false })
            writer.Write('?');

        return TypeNameResult.Success;
    }

    private static TypeNameResult TryWriteContainingTypeName(
        PooledStringWriter writer,
        INamedTypeSymbol typeSymbol,
        IReadOnlyDictionary<ITypeParameterSymbol, string> typeParameterMappings
    ) {
        if(typeSymbol.ContainingType is null) {
            WriteNamespaceName(writer, typeSymbol.ContainingNamespace);
            return TypeNameResult.Success;
        }

        var result = TryWriteTypeName(writer, typeSymbol.ContainingType, typeParameterMappings);
        if(result.IsFailure)
            return result;

        writer.Write('.');
        return TypeNameResult.Success;
    }

    private static void WriteNamespaceName(PooledStringWriter writer, INamespaceSymbol ns) {
        if(ns.IsGlobalNamespace) {
            writer.Write("global::");
        } else {
            WriteNamespaceName(writer, ns.ContainingNamespace);
            writer.Write(ns.Name);
            writer.Write('.');
        }
    }
}
