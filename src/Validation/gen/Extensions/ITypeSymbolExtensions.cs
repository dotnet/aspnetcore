// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

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
            // Remove the nullable annotation but keep any generic arguments, e.g. List<int>? → List<int>
            // so we can retain them in future steps.
            type = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
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
    internal static bool IsExemptType(this ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        return SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_HttpContext))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_HttpRequest))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_HttpResponse))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Threading_CancellationToken))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_IFormCollection))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_IFormFileCollection))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_IFormFile))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_IO_Stream))
               || SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_IO_Pipelines_PipeReader));
    }

    internal static IPropertySymbol? FindPropertyIncludingBaseTypes(this INamedTypeSymbol typeSymbol, string propertyName)
    {
        var property = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => string.Equals(p.Name, propertyName, System.StringComparison.OrdinalIgnoreCase));

        if (property != null)
        {
            return property;
        }

        // If not found, recursively search base types
        if (typeSymbol.BaseType is INamedTypeSymbol baseType)
        {
            return FindPropertyIncludingBaseTypes(baseType, propertyName);
        }

        return null;
    }

    /// <summary>
    /// Checks if the parameter is marked with [FromService] or [FromKeyedService] attributes.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <param name="fromServiceMetadataSymbol">The symbol representing the [FromService] attribute.</param>
    /// <param name="fromKeyedServiceAttributeSymbol">The symbol representing the [FromKeyedService] attribute.</param>
    internal static bool IsServiceParameter(this IParameterSymbol parameter, INamedTypeSymbol fromServiceMetadataSymbol, INamedTypeSymbol fromKeyedServiceAttributeSymbol)
    {
        return parameter.GetAttributes().Any(attr =>
            attr.AttributeClass is not null &&
            (attr.AttributeClass.ImplementsInterface(fromServiceMetadataSymbol) ||
             SymbolEqualityComparer.Default.Equals(attr.AttributeClass, fromKeyedServiceAttributeSymbol)));
    }
}
