// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

internal static class ISymbolExtensions
{
    /// <summary>
    /// Resolves display information for a symbol using the following order of precedence:
    /// <list type="number">
    ///   <item><description><see cref="T:System.ComponentModel.DataAnnotations.DisplayAttribute"/> with both
    ///   <c>ResourceType</c> and <c>Name</c> set — name resolved via the resource-based accessor.</description></item>
    ///   <item><description><see cref="T:System.ComponentModel.DataAnnotations.DisplayAttribute"/> with <c>Name</c>
    ///   only — name resolved as a string literal.</description></item>
    ///   <item><description><see cref="T:System.ComponentModel.DisplayNameAttribute"/> — name resolved as a string literal (no resource support).</description></item>
    ///   <item><description>Falls back to <see langword="null"/> so the caller can use the member name.</description></item>
    /// </list>
    /// </summary>
    public static DisplayInfo? GetDisplayInfo(this ISymbol symbol, INamedTypeSymbol displayAttributeSymbol, INamedTypeSymbol? displayNameAttributeSymbol)
    {
        AttributeData? displayAttribute = null;
        AttributeData? displayNameAttribute = null;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is { } attributeClass)
            {
                if (SymbolEqualityComparer.Default.Equals(attributeClass, displayAttributeSymbol))
                {
                    displayAttribute = attribute;
                }
                else if (displayNameAttributeSymbol is not null &&
                    SymbolEqualityComparer.Default.Equals(attributeClass, displayNameAttributeSymbol))
                {
                    displayNameAttribute = attribute;
                }
            }
        }

        if (displayAttribute is not null && !displayAttribute.NamedArguments.IsDefaultOrEmpty)
        {
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

            if (name is not null)
            {
                return new DisplayInfo(name, resourceType);
            }
        }

        // Fall back to DisplayNameAttribute if DisplayAttribute didn't provide a name.
        if (displayNameAttribute is not null &&
            !displayNameAttribute.ConstructorArguments.IsDefaultOrEmpty &&
            displayNameAttribute.ConstructorArguments[0].Value is string displayName)
        {
            return new DisplayInfo(displayName, ResourceType: null);
        }

        return null;
    }

    public static bool IsEqualityContract(this IPropertySymbol prop, WellKnownTypes wellKnownTypes) =>
        prop.Name == "EqualityContract"
        && SymbolEqualityComparer.Default.Equals(prop.Type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Type))
        && prop.DeclaredAccessibility == Accessibility.Protected;
}
