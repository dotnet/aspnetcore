// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

internal static class ISymbolExtensions
{
    public static bool IsEqualityContract(this IPropertySymbol prop, WellKnownTypes wellKnownTypes) =>
        prop.Name == "EqualityContract"
        && SymbolEqualityComparer.Default.Equals(prop.Type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Type))
        && prop.DeclaredAccessibility == Accessibility.Protected;

    public static DisplayInfo? GetDisplayInfo(this ISymbol symbol, INamedTypeSymbol displayAttributeSymbol)
    {
        AttributeData? displayAttribute = null;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is { } attributeClass &&
                SymbolEqualityComparer.Default.Equals(attributeClass, displayAttributeSymbol))
            {
                displayAttribute = attribute;
                break;
            }
        }

        if (displayAttribute is null || displayAttribute.NamedArguments.IsDefaultOrEmpty)
        {
            return null;
        }

        string? name = null;
        INamedTypeSymbol? resourceType = null;

        foreach (var namedArg in displayAttribute.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Name":
                    name = namedArg.Value.Value as string;
                    break;

                case "ResourceType":
                    resourceType = namedArg.Value.Value as INamedTypeSymbol;
                    break;
            }
        }

        return name is not null ? new DisplayInfo(name, resourceType) : null;
    }
}
