// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

internal static class ISymbolExtensions
{
    /// <summary>
    /// Inspects the symbol's <c>[Display]</c> and <c>[DisplayName]</c> attributes and returns
    /// (a) the literal display name to bake into the generated metadata, and
    /// (b) whether <c>[Display]</c> uses <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.ResourceType"/>
    /// (in which case the generator must emit a runtime accessor for the attribute instead of a literal).
    /// </summary>
    public static (string? LiteralDisplayName, bool HasResourceDisplayAttribute) GetDisplayInfo(
        this ISymbol symbol,
        INamedTypeSymbol displayAttributeSymbol,
        INamedTypeSymbol displayNameAttributeSymbol)
    {
        var displayAttr = symbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass is { } ac && SymbolEqualityComparer.Default.Equals(ac, displayAttributeSymbol));

        if (displayAttr is not null)
        {
            string? name = null;
            bool hasResourceType = false;

            foreach (var arg in displayAttr.NamedArguments)
            {
                if (string.Equals(arg.Key, "Name", StringComparison.Ordinal))
                {
                    name = arg.Value.Value as string;
                }
                else if (string.Equals(arg.Key, "ResourceType", StringComparison.Ordinal))
                {
                    hasResourceType = arg.Value.Value is INamedTypeSymbol;
                }
            }

            // ResourceType is only meaningful when Name is also set; DisplayAttribute.GetName()
            // returns null otherwise.
            if (hasResourceType && name is not null)
            {
                // Defer to DisplayAttribute.GetName() at runtime; do not bake the literal Name.
                return (LiteralDisplayName: null, HasResourceDisplayAttribute: true);
            }

            // [Display(Name = "X")] without ResourceType → bake literal.
            if (name is not null)
            {
                return (LiteralDisplayName: name, HasResourceDisplayAttribute: false);
            }
        }

        var displayNameAttr = symbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass is { } ac && SymbolEqualityComparer.Default.Equals(ac, displayNameAttributeSymbol));

        if (displayNameAttr is not null
            && !displayNameAttr.ConstructorArguments.IsDefaultOrEmpty
            && displayNameAttr.ConstructorArguments[0].Value is string dn)
        {
            // [DisplayName("X")] → bake literal. (No ResourceType to consider.)
            return (LiteralDisplayName: dn, HasResourceDisplayAttribute: false);
        }

        return (LiteralDisplayName: null, HasResourceDisplayAttribute: false);
    }

    public static bool IsEqualityContract(this IPropertySymbol prop, WellKnownTypes wellKnownTypes) =>
        prop.Name == "EqualityContract"
        && SymbolEqualityComparer.Default.Equals(prop.Type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Type))
        && prop.DeclaredAccessibility == Accessibility.Protected;
}
