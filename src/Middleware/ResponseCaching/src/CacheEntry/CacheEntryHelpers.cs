// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching;

internal static class CacheEntryHelpers
{
    internal static long EstimateCachedResponseSize(CachedResponse cachedResponse)
    {
        if (cachedResponse == null)
        {
            return 0L;
        }

        checked
        {
            // StatusCode
            long size = sizeof(int);

            // Headers
            if (cachedResponse.Headers != null)
            {
                foreach (var item in cachedResponse.Headers)
                {
                    size += (item.Key.Length * sizeof(char)) + EstimateStringValuesSize(item.Value);
                }
            }

            // Body
            if (cachedResponse.Body != null)
            {
                size += cachedResponse.Body.Length;
            }

            return size;
        }
    }

    internal static long EstimateCachedVaryByRulesySize(CachedVaryByRules? cachedVaryByRules)
    {
        if (cachedVaryByRules == null)
        {
            return 0L;
        }

        checked
        {
            var size = 0L;

            // VaryByKeyPrefix
            if (!string.IsNullOrEmpty(cachedVaryByRules.VaryByKeyPrefix))
            {
                size = cachedVaryByRules.VaryByKeyPrefix.Length * sizeof(char);
            }

            // Headers
            size += EstimateStringValuesSize(cachedVaryByRules.Headers);

            // QueryKeys
            size += EstimateStringValuesSize(cachedVaryByRules.QueryKeys);

            return size;
        }
    }

    internal static long EstimateStringValuesSize(StringValues stringValues)
    {
        checked
        {
            var size = 0L;

            for (var i = 0; i < stringValues.Count; i++)
            {
                var stringValue = stringValues[i];
                if (!string.IsNullOrEmpty(stringValue))
                {
                    size += stringValue.Length * sizeof(char);
                }
            }

            return size;
        }
    }
}
