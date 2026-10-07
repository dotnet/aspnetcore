// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal sealed class TupleContractManifest
{
    private const string JsonConverterAttributeName = "System.Text.Json.Serialization.JsonConverterAttribute";
    private const string JsonIgnoreAttributeName = "System.Text.Json.Serialization.JsonIgnoreAttribute";
    private const string JsonPolymorphicAttributeName = "System.Text.Json.Serialization.JsonPolymorphicAttribute";
    private const string JsonDerivedTypeAttributeName = "System.Text.Json.Serialization.JsonDerivedTypeAttribute";

    private readonly HashSet<ITypeSymbol> _visited = new(SymbolEqualityComparer.Default);
    private readonly Dictionary<string, string> _factories = new(StringComparer.Ordinal);

    private TupleContractManifest(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    public bool IsEnabled { get; }

    public ImmutableArray<string> FactoryExpressions => _factories
        .OrderBy(entry => entry.Key, StringComparer.Ordinal)
        .Select(entry => entry.Value)
        .ToImmutableArray();

    public static TupleContractManifest Create(ImmutableArray<Endpoint> endpoints)
    {
        var isEnabled = endpoints.Any(endpoint => endpoint.SupportsGeneratedTupleConverters);
        var manifest = new TupleContractManifest(isEnabled);
        if (!isEnabled)
        {
            return manifest;
        }

        foreach (var endpoint in endpoints)
        {
            if (endpoint.Response is { IsSerializable: true, ResponseType: { } responseType })
            {
                manifest.Visit(responseType);
            }

            foreach (var parameter in endpoint.Parameters)
            {
                manifest.VisitParameter(parameter);
            }
        }

        return manifest;
    }

    private void Visit(ITypeSymbol type)
    {
        type = UnwrapNullable(type);
        if (type is INamedTypeSymbol { IsTupleType: true, TupleUnderlyingType: { } underlyingType })
        {
            type = underlyingType;
        }
        if (!_visited.Add(type))
        {
            return;
        }

        if (type is IArrayTypeSymbol array)
        {
            Visit(array.ElementType);
            return;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return;
        }

        if (TryCreateTupleFactory(namedType, out var factory))
        {
            _factories[factory] = factory;
            VisitTupleElementTypes(namedType);
            return;
        }

        if (!IsSupportedDto(namedType))
        {
            return;
        }

        if (namedType.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
        {
            Visit(baseType);
        }

        foreach (var property in namedType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property =>
                !property.IsStatic &&
                !property.IsIndexer &&
                property.DeclaredAccessibility == Accessibility.Public &&
                property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            if (IsAlwaysIgnored(property) ||
                HasAttribute(property, JsonConverterAttributeName))
            {
                continue;
            }

            Visit(property.Type);
        }
    }

    private void VisitTupleElementTypes(INamedTypeSymbol tupleType)
    {
        if (tupleType is { IsTupleType: true, TupleUnderlyingType: { } underlyingType })
        {
            tupleType = underlyingType;
        }

        var typeArguments = tupleType.TypeArguments;
        var directElementCount = typeArguments.Length == 8 ? 7 : typeArguments.Length;
        for (var i = 0; i < directElementCount; i++)
        {
            Visit(typeArguments[i]);
        }

        if (typeArguments.Length == 8 && typeArguments[7] is INamedTypeSymbol restType)
        {
            VisitTupleElementTypes(restType);
        }
    }

    private void VisitParameter(EndpointParameter parameter)
    {
        if (parameter.Source == EndpointParameterSource.JsonBody)
        {
            Visit(parameter.Type);
        }

        if (parameter is { Source: EndpointParameterSource.AsParameters, EndpointParameters: { } innerParameters })
        {
            foreach (var innerParameter in innerParameters)
            {
                VisitParameter(innerParameter);
            }
        }
    }

    private static bool IsSupportedDto(INamedTypeSymbol type)
        => type.Locations.Any(location => location.IsInSource) &&
            !type.IsUnboundGenericType &&
            !type.TypeArguments.Any(argument => argument.TypeKind == TypeKind.TypeParameter) &&
            type.TypeKind is TypeKind.Class or TypeKind.Struct &&
            !HasAttribute(type, JsonConverterAttributeName) &&
            !HasAttribute(type, JsonPolymorphicAttributeName) &&
            !HasAttribute(type, JsonDerivedTypeAttributeName);

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        return type;
    }

    private static bool TryCreateTupleFactory(INamedTypeSymbol type, out string factory)
    {
        factory = string.Empty;
        if (type is { IsTupleType: true, TupleUnderlyingType: { } underlyingType })
        {
            type = underlyingType;
        }

        if (type.ContainingNamespace.ToDisplayString() != "System")
        {
            return false;
        }

        var methodName = type.OriginalDefinition.MetadataName switch
        {
            "ValueTuple" => "CreateValueTuple",
            "ValueTuple`1" or "ValueTuple`2" or "ValueTuple`3" or "ValueTuple`4" or
            "ValueTuple`5" or "ValueTuple`6" or "ValueTuple`7" or "ValueTuple`8" => "CreateValueTuple",
            "Tuple`1" or "Tuple`2" or "Tuple`3" or "Tuple`4" or
            "Tuple`5" or "Tuple`6" or "Tuple`7" or "Tuple`8" => "CreateTuple",
            _ => null,
        };
        if (methodName is null)
        {
            return false;
        }

        var typeArguments = type.TypeArguments;
        var displayedTypeArguments = string.Join(
            ", ",
            typeArguments.Select(argument => argument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        var genericArguments = typeArguments.Length == 0 ? string.Empty : $"<{displayedTypeArguments}>";
        var restArgument = string.Empty;
        if (typeArguments.Length == 8)
        {
            if (typeArguments[7] is not INamedTypeSymbol restType ||
                !TryCreateTupleFactory(restType, out var restFactory))
            {
                return false;
            }

            restArgument = restFactory;
        }

        factory = $"global::Microsoft.AspNetCore.OpenApi.JsonArrayTupleConverters.{methodName}{genericArguments}({restArgument})";
        return true;
    }

    private static bool HasAttribute(ISymbol symbol, string metadataName)
        => symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == metadataName);

    private static bool IsAlwaysIgnored(IPropertySymbol property)
    {
        var attribute = property.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.ToDisplayString() == JsonIgnoreAttributeName);
        if (attribute is null)
        {
            return false;
        }

        var condition = attribute.NamedArguments.FirstOrDefault(argument => argument.Key == "Condition");
        if (condition.Key is null)
        {
            return true;
        }

        var always = condition.Value.Type?.GetMembers("Always")
            .OfType<IFieldSymbol>()
            .SingleOrDefault()
            ?.ConstantValue;
        return Equals(condition.Value.Value, always);
    }
}
