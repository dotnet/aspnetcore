// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

internal static class ISymbolExtensions
{
    public static string? GetDisplayName(this ISymbol property, INamedTypeSymbol displayAttribute)
    {
        var displayNameAttribute = property.GetAttributes()
            .FirstOrDefault(attribute =>
                attribute.AttributeClass is { } attributeClass &&
                SymbolEqualityComparer.Default.Equals(attributeClass, displayAttribute));

        if (displayNameAttribute is not null)
        {
            // For the [Foo(Name = "bar")] case
            if (!displayNameAttribute.NamedArguments.IsDefaultOrEmpty)
            {
                foreach (var namedArgument in displayNameAttribute.NamedArguments)
                {
                    if (string.Equals(namedArgument.Key, "Name", StringComparison.Ordinal))
                    {
                        if (namedArgument.Value.Value?.ToString() is { } name)
                        {
                            return name;
                        }
                    }
                }
            }

            // For the [Foo("bar")] case
            if (displayNameAttribute.ConstructorArguments.Length is 1)
            {
                var arg = displayNameAttribute.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string name)
                {
                    return name;
                }
            }
        }

        return null;
    }

    public static bool IsEqualityContract(this IPropertySymbol prop, WellKnownTypes wellKnownTypes) =>
        prop.Name == "EqualityContract"
        && SymbolEqualityComparer.Default.Equals(prop.Type, wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Type))
        && prop.DeclaredAccessibility == Accessibility.Protected;
}
