// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints;

// TODO: This is shared from MVC. We should move it to a shared location.
internal static class ClosedGenericMatcher
{
    public static Type? ExtractGenericInterface(Type queryType, Type interfaceType)
    {
        ArgumentNullException.ThrowIfNull(queryType);
        ArgumentNullException.ThrowIfNull(interfaceType);

        if (IsGenericInstantiation(queryType, interfaceType))
        {
            // queryType matches (i.e. is a closed generic type created from) the open generic type.
            return queryType;
        }

        // Otherwise check all interfaces the type implements for a match.
        // - If multiple different generic instantiations exists, we want the most derived one.
        // - If that doesn't break the tie, then we sort alphabetically so that it's deterministic.
        //
        // We do this by looking at interfaces on the type, and recursing to the base type
        // if we don't find any matches.
        return GetGenericInstantiation(queryType, interfaceType);
    }

    private static bool IsGenericInstantiation(Type candidate, Type interfaceType)
    {
        return
            candidate.IsGenericType &&
            candidate.GetGenericTypeDefinition() == interfaceType;
    }

    [SuppressMessage(
        "Trimming",
        "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "All bindable types are meant to be defined in assemblies where those types are preserved.")]
    private static Type? GetGenericInstantiation(Type queryType, Type interfaceType)
    {
        Type? bestMatch = null;
        var interfaces = queryType.GetInterfaces();
        foreach (var @interface in interfaces)
        {
            if (IsGenericInstantiation(@interface, interfaceType))
            {
                if (bestMatch == null)
                {
                    bestMatch = @interface;
                }
                else if (StringComparer.Ordinal.Compare(@interface.FullName, bestMatch.FullName) < 0)
                {
                    bestMatch = @interface;
                }
                else
                {
                    // There are two matches at this level of the class hierarchy, but @interface is after
                    // bestMatch in the sort order.
                }
            }
        }

        if (bestMatch != null)
        {
            return bestMatch;
        }

        // BaseType will be null for object and interfaces, which means we've reached 'bottom'.
        var baseType = queryType?.BaseType;
        if (baseType == null)
        {
            return null;
        }
        else
        {
            return GetGenericInstantiation(baseType, interfaceType);
        }
    }
}
