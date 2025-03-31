// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal static class ITypeSymbolExtensions
{
    public static bool IsEnumerable(this ITypeSymbol type, INamedTypeSymbol enumerable)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        return type.ImplementsInterface(enumerable) || SymbolEqualityComparer.Default.Equals(type, enumerable);
    }

    public static bool ImplementsValidationAttribute(this ITypeSymbol typeSymbol, INamedTypeSymbol validationAttributeSymbol)
    {
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, validationAttributeSymbol))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    public static ITypeSymbol UnwrapType(this ITypeSymbol type, INamedTypeSymbol enumerable)
    {
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol { TypeArguments.Length: 1 })
        {
            // Extract the T from a Nullable<T>
            type = ((INamedTypeSymbol)type).TypeArguments[0];
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            // Extract the underlying type from a reference type
            type = type.OriginalDefinition;
        }

        if (type is INamedTypeSymbol namedType && namedType.IsEnumerable(enumerable) && namedType.TypeArguments.Length == 1)
        {
            // Extract the T from an IEnumerable<T> or List<T>
            type = namedType.TypeArguments[0];
        }

        return type;
    }

    internal static bool ImplementsInterface(this ITypeSymbol type, ITypeSymbol interfaceType)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceType, iface))
            {
                return true;
            }
        }
        return false;
    }

    internal static ImmutableArray<INamedTypeSymbol>? GetJsonDerivedTypes(this ITypeSymbol type, INamedTypeSymbol jsonDerivedTypeAttribute)
    {
        var derivedTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (var attribute in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, jsonDerivedTypeAttribute))
            {
                var derivedType = (INamedTypeSymbol?)attribute.ConstructorArguments[0].Value;
                if (derivedType is not null && !SymbolEqualityComparer.Default.Equals(derivedType, type))
                {
                    derivedTypes.Add(derivedType);
                }
            }
        }

        return derivedTypes.Count == 0 ? null : derivedTypes.ToImmutable();
    }

    // Types exempted here have special binding rules in RDF and RDG and are not validatable
    // types themselves so we short-circuit on them.
    internal static bool IsExemptType(this ITypeSymbol type, RequiredSymbols requiredSymbols)
    {
        return SymbolEqualityComparer.Default.Equals(type, requiredSymbols.HttpContext)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.HttpRequest)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.HttpResponse)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.CancellationToken)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.IFormCollection)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.IFormFileCollection)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.IFormFile)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.Stream)
               || SymbolEqualityComparer.Default.Equals(type, requiredSymbols.PipeReader);
    }
}
