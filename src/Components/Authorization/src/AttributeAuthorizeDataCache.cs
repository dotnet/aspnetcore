// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization;

internal static class AttributeAuthorizeDataCache
{
    private static readonly ConcurrentDictionary<Type, IAuthorizeData[]?> _cache = new();

    public static IAuthorizeData[]? GetAuthorizeDataForType(Type type)
    {
        if (!_cache.TryGetValue(type, out var result))
        {
            result = ComputeAuthorizeDataForType(type);
            _cache[type] = result; // Safe race - doesn't matter if it overwrites
        }

        return result;
    }

    private static IAuthorizeData[]? ComputeAuthorizeDataForType(Type type)
    {
        // Allow Anonymous skips all authorization
        var allAttributes = type.GetCustomAttributes(inherit: true);
        List<IAuthorizeData>? authorizeDatas = null;
        for (var i = 0; i < allAttributes.Length; i++)
        {
            if (allAttributes[i] is IAllowAnonymous)
            {
                return null;
            }

            if (allAttributes[i] is IAuthorizeData authorizeData)
            {
                authorizeDatas ??= new();
                authorizeDatas.Add(authorizeData);
            }
        }

        return authorizeDatas?.ToArray();
    }
}
