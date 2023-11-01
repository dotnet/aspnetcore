// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class SymbolExtensions
{
    public static ITypeSymbol UnwrapTypeSymbol(this ITypeSymbol typeSymbol, bool unwrapArray = false, bool unwrapNullable = false)
    {
        INamedTypeSymbol? unwrappedTypeSymbol = null;

        // If it is an array, and unwrapArray = true, unwrap it before unwrapping nullable.
        if (unwrapArray && typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            unwrappedTypeSymbol = arrayTypeSymbol.ElementType as INamedTypeSymbol;
        }
        else if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            unwrappedTypeSymbol = namedTypeSymbol;
        }

        // If it is nullable, unwrap it.
        if (unwrapNullable && unwrappedTypeSymbol?.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            unwrappedTypeSymbol = unwrappedTypeSymbol.TypeArguments[0] as INamedTypeSymbol;
        }

        return unwrappedTypeSymbol ?? typeSymbol;
    }

    public static IEnumerable<ITypeSymbol> GetThisAndBaseTypes(this ITypeSymbol? type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasAttribute(this ImmutableArray<AttributeData> attributes, INamedTypeSymbol attributeType)
    {
        return attributes.TryGetAttribute(attributeType, out _);
    }

    public static bool TryGetAttribute(this ImmutableArray<AttributeData> attributes, INamedTypeSymbol attributeType, [NotNullWhen(true)] out AttributeData? matchedAttribute)
    {
        foreach (var attributeData in attributes)
        {
            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeType))
            {
                matchedAttribute = attributeData;
                return true;
            }
        }

        matchedAttribute = null;
        return false;
    }

    public static bool HasAttributeImplementingInterface(this ISymbol symbol, INamedTypeSymbol interfaceType)
    {
        return symbol.TryGetAttributeImplementingInterface(interfaceType, out var _);
    }

    public static bool TryGetAttributeImplementingInterface(this ISymbol symbol, INamedTypeSymbol interfaceType, [NotNullWhen(true)] out AttributeData? matchedAttribute)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass is not null && attributeData.AttributeClass.Implements(interfaceType))
            {
                matchedAttribute = attributeData;
                return true;
            }
        }

        matchedAttribute = null;
        return false;
    }

    public static bool HasAttributeImplementingInterface(this ImmutableArray<AttributeData> attributes, INamedTypeSymbol interfaceType)
    {
        return attributes.TryGetAttributeImplementingInterface(interfaceType, out var _);
    }

    public static bool TryGetAttributeImplementingInterface(this ImmutableArray<AttributeData> attributes, INamedTypeSymbol interfaceType, [NotNullWhen(true)] out AttributeData? matchedAttribute)
    {
        foreach (var attributeData in attributes)
        {
            if (attributeData.AttributeClass is not null && attributeData.AttributeClass.Implements(interfaceType))
            {
                matchedAttribute = attributeData;
                return true;
            }
        }

        matchedAttribute = null;
        return false;
    }

    public static bool Implements(this ITypeSymbol type, ITypeSymbol interfaceType)
    {
        foreach (var t in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(t, interfaceType))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsType(this INamedTypeSymbol type, string typeName, SemanticModel semanticModel)
        => SymbolEqualityComparer.Default.Equals(type, semanticModel.Compilation.GetTypeByMetadataName(typeName));

    public static bool IsType(this INamedTypeSymbol type, INamedTypeSymbol otherType)
        => SymbolEqualityComparer.Default.Equals(type, otherType);

    public static ITypeSymbol GetParameterType(this ISymbol symbol)
    {
        return symbol switch
        {
            IParameterSymbol parameterSymbol => parameterSymbol.Type,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => throw new InvalidOperationException("Unexpected symbol type: " + symbol)
        };
    }

    public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
        => symbol switch
        {
            IMethodSymbol methodSymbol => methodSymbol.Parameters,
            IPropertySymbol parameterSymbol => parameterSymbol.Parameters,
            _ => ImmutableArray<IParameterSymbol>.Empty,
        };

    public static ISymbol? GetAnySymbol(this SymbolInfo info)
        => info.Symbol ?? info.CandidateSymbols.FirstOrDefault();

    public static bool IsOptional(this IParameterSymbol parameterSymbol) =>
        parameterSymbol.Type is INamedTypeSymbol
        {
            NullableAnnotation: NullableAnnotation.Annotated
        } || parameterSymbol.HasExplicitDefaultValue;

    public static bool IsOptional(this IPropertySymbol propertySymbol) =>
        propertySymbol.Type is INamedTypeSymbol
        {
            NullableAnnotation: NullableAnnotation.Annotated
        } && !propertySymbol.IsRequired;

    public static string GetDefaultValueString(this IParameterSymbol parameterSymbol)
    {
        return !parameterSymbol.HasExplicitDefaultValue
            ? "null"
            : InnerGetDefaultValueString(parameterSymbol.ExplicitDefaultValue);
    }

    private static string InnerGetDefaultValueString(object? defaultValue)
    {
        return defaultValue switch
        {
            string s => SymbolDisplay.FormatLiteral(s, true),
            char c => SymbolDisplay.FormatLiteral(c, true),
            bool b => b ? "true" : "false",
            null => "default",
            float f when f is float.NegativeInfinity => "float.NegativeInfinity",
            float f when f is float.PositiveInfinity => "float.PositiveInfinity",
            float f when f is float.NaN => "float.NaN",
            float f => $"{SymbolDisplay.FormatPrimitive(f, false, false)}F",
            double d when d is double.NegativeInfinity => "double.NegativeInfinity",
            double d when d is double.PositiveInfinity => "double.PositiveInfinity",
            double d when d is double.NaN => "double.NaN",
            decimal d => $"{SymbolDisplay.FormatPrimitive(d, false, false)}M",
            _ => SymbolDisplay.FormatPrimitive(defaultValue, false, false),
        };
    }

    public static bool TryGetNamedArgumentValue<T>(this AttributeData attribute, string argumentName, out T? argumentValue)
    {
        argumentValue = default;
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (string.Equals(namedArgument.Key, argumentName, StringComparison.Ordinal))
            {
                var routeParameterNameConstant = namedArgument.Value;
                argumentValue = (T?)routeParameterNameConstant.Value;
                return true;
            }
        }
        return false;
    }

    public static string GetParameterInfoFromConstructorCode(this IParameterSymbol parameterSymbol)
    {
        if (parameterSymbol is { ContainingSymbol: IMethodSymbol constructor })
        {
            var constructedType = $"typeof({parameterSymbol.ContainingType.ToDisplayString()})";
            var parameterTypes = constructor.Parameters.Select(parameter => $"typeof({parameter.Type.ToDisplayString()})");
            var parameterTypesString = string.Join(", ", parameterTypes);
            var getConstructorParameters = $$"""new[] { {{parameterTypesString}} }""";
            return $"{constructedType}.GetConstructor({getConstructorParameters})?.GetParameters()[{parameterSymbol.Ordinal}]";
        }
        return "null";
    }
}
