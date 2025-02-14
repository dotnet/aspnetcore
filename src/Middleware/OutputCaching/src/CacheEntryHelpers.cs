// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

internal static class CacheEntryHelpers
{
    internal static long EstimateCachedResponseSize(OutputCacheEntry cachedResponse)
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
            foreach (var item in cachedResponse.Headers.Span)
            {
                size += (item.Name.Length * sizeof(char)) + EstimateStringValuesSize(item.Value);
            }

            // Body
            size += cachedResponse.Body.Length;

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
