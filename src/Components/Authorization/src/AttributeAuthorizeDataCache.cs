// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.HotReload;

namespace Microsoft.AspNetCore.Components.Authorization;

internal static class AttributeAuthorizeDataCache
{
    static AttributeAuthorizeDataCache()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCache;
        }
    }

    private static readonly ConcurrentDictionary<Type, (IAuthorizeData[]? AuthorizeData, IAuthorizationRequirementData[]? RequirementData)> _cache = new();

    private static void ClearCache() => _cache.Clear();

    public static IAuthorizeData[]? GetAuthorizeDataForType(Type type)
        => GetAuthorizationDataForType(type).AuthorizeData;

    public static IAuthorizationRequirementData[]? GetAuthorizationRequirementDataForType(Type type)
        => GetAuthorizationDataForType(type).RequirementData;

    private static (IAuthorizeData[]? AuthorizeData, IAuthorizationRequirementData[]? RequirementData) GetAuthorizationDataForType(Type type)
    {
        if (!_cache.TryGetValue(type, out var result))
        {
            result = ComputeAuthorizationDataForType(type);
            _cache[type] = result; // Safe race - doesn't matter if it overwrites
        }

        return result;
    }

    private static (IAuthorizeData[]? AuthorizeData, IAuthorizationRequirementData[]? RequirementData) ComputeAuthorizationDataForType(Type type)
    {
        // Allow Anonymous skips all authorization
        var allAttributes = type.GetCustomAttributes(inherit: true);
        List<IAuthorizeData>? authorizeDatas = null;
        List<IAuthorizationRequirementData>? requirementDatas = null;
        for (var i = 0; i < allAttributes.Length; i++)
        {
            if (allAttributes[i] is IAllowAnonymous)
            {
                return (null, null);
            }

            if (allAttributes[i] is IAuthorizeData authorizeData)
            {
                authorizeDatas ??= new();
                authorizeDatas.Add(authorizeData);
            }

            if (allAttributes[i] is IAuthorizationRequirementData requirementData)
            {
                requirementDatas ??= new();
                requirementDatas.Add(requirementData);
            }
        }

        return (authorizeDatas?.ToArray(), requirementDatas?.ToArray());
    }
}
