// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection.Internal;

internal sealed class DefaultTypeNameResolver : ITypeNameResolver
{
    public static readonly DefaultTypeNameResolver Instance = new();

    private DefaultTypeNameResolver()
    {
    }

    [UnconditionalSuppressMessage("Trimmer", "IL2057", Justification = "Type.GetType is only used to resolve statically known types that are referenced by DataProtection assembly.")]
    public bool TryResolveType(string typeName, [NotNullWhen(true)] out Type? type)
    {
        try
        {
            // Some exceptions are thrown regardless of the value of throwOnError.
            // For example, if the type is found but cannot be loaded,
            // a System.TypeLoadException is thrown even if throwOnError is false.
            type = Type.GetType(typeName, throwOnError: false);
            return type != null;
        }
        catch
        {
            type = null;
            return false;
        }
    }
}
