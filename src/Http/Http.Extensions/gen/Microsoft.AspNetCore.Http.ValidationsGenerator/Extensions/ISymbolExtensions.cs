// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal static class ISymbolExtensions
{
    public static string GetDisplayName(this ISymbol property, INamedTypeSymbol displayAttribute)
    {
        var displayNameAttribute = property.GetAttributes()
            .FirstOrDefault(attribute =>
                attribute.AttributeClass is { } attributeClass &&
                SymbolEqualityComparer.Default.Equals(attributeClass, displayAttribute));

        if (displayNameAttribute is not null)
        {
            if (!displayNameAttribute.NamedArguments.IsDefaultOrEmpty)
            {
                foreach (var namedArgument in displayNameAttribute.NamedArguments)
                {
                    if (string.Equals(namedArgument.Key, "Name", StringComparison.Ordinal))
                    {
                        return namedArgument.Value.Value?.ToString() ?? property.Name;
                    }
                }
            }
        }

        return property.Name;
    }
}
