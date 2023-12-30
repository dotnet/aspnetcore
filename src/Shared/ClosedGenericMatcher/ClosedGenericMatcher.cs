// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Internal;

/// <summary>
/// Helper related to generic interface definitions and implementing classes.
/// </summary>
internal static class ClosedGenericMatcher
{
    /// <summary>
    /// Determine whether <paramref name="queryType"/> is or implements a closed generic <see cref="Type"/>
    /// created from <paramref name="interfaceType"/>.
    /// </summary>
    /// <param name="queryType">The <see cref="Type"/> of interest.</param>
    /// <param name="interfaceType">The open generic <see cref="Type"/> to match. Usually an interface.</param>
    /// <returns>
    /// The closed generic <see cref="Type"/> created from <paramref name="interfaceType"/> that
    /// <paramref name="queryType"/> is or implements. <c>null</c> if the two <see cref="Type"/>s have no such
    /// relationship.
    /// </returns>
    /// <remarks>
    /// This method will return <paramref name="queryType"/> if <paramref name="interfaceType"/> is
    /// <c>typeof(KeyValuePair{,})</c>, and <paramref name="queryType"/> is
    /// <c>typeof(KeyValuePair{string, object})</c>.
    /// </remarks>
    public static Type? ExtractGenericInterface([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]  Type queryType, Type interfaceType)
    {
#if !NET8_0_OR_GREATER
        ArgumentNullThrowHelper.ThrowIfNull(queryType);
        ArgumentNullThrowHelper.ThrowIfNull(interfaceType);
#else
        ArgumentNullException.ThrowIfNull(queryType);
        ArgumentNullException.ThrowIfNull(interfaceType);
#endif

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

    private static Type? GetGenericInstantiation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type queryType, Type interfaceType)
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
