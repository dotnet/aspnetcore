// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.DataProtection.Internal;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Helpful extension methods on <see cref="Type"/>.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Throws <see cref="InvalidCastException"/> if <paramref name="implementationType"/>
    /// is not assignable to <paramref name="expectedBaseType"/>.
    /// </summary>
    public static void AssertIsAssignableFrom(this Type expectedBaseType, Type implementationType)
    {
        if (!expectedBaseType.IsAssignableFrom(implementationType))
        {
            // It might seem a bit weird to throw an InvalidCastException explicitly rather than
            // to let the CLR generate one, but searching through NetFX there is indeed precedent
            // for this pattern when the caller knows ahead of time the operation will fail.
            throw new InvalidCastException(Resources.FormatTypeExtensions_BadCast(
                expectedBaseType.AssemblyQualifiedName, implementationType.AssemblyQualifiedName));
        }
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2057", Justification = "Unknown type names are rarely used by apps. Handle trimmed types by providing a useful error message.")]
    public static Type GetTypeWithTrimFriendlyErrorMessage(string typeName)
    {
        try
        {
            return Type.GetType(typeName, throwOnError: true)!;
        }
        catch (TypeLoadException ex)
        {
            throw new InvalidOperationException($"Unable to load type '{typeName}'. If the app is published with trimming then this type may have been trimmed. Ensure the type's assembly is excluded from trimming.", ex);
        }
    }

    public static bool MatchName(this Type matchType, string resolvedTypeName, ITypeNameResolver typeNameResolver)
    {
        // Before attempting to resolve the name to a type, check if it starts with the full name of the type.
        // Use StartsWith to ignore potential assembly version differences.
        if (matchType.FullName != null && resolvedTypeName.StartsWith(matchType.FullName, StringComparison.Ordinal))
        {
            return typeNameResolver.TryResolveType(resolvedTypeName, out var resolvedType) && resolvedType == matchType;
        }

        return false;
    }
}
