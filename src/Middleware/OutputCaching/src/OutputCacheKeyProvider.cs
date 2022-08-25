// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCacheKeyProvider : IOutputCacheKeyProvider
{
    // Use the record separator for delimiting components of the cache key to avoid possible collisions
    private const char KeyDelimiter = '\x1e';
    // Use the unit separator for delimiting subcomponents of the cache key to avoid possible collisions
    private const char KeySubDelimiter = '\x1f';

    private readonly ObjectPool<StringBuilder> _builderPool;
    private readonly OutputCacheOptions _options;

    internal OutputCacheKeyProvider(ObjectPoolProvider poolProvider, IOptions<OutputCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(poolProvider);
        ArgumentNullException.ThrowIfNull(options);

        _builderPool = poolProvider.CreateStringBuilderPool();
        _options = options.Value;
    }

    // <VaryByKeyPrefix><delimiter>
    // GET<delimiter>SCHEME<delimiter>HOST:PORT/PATHBASE/PATH<delimiter>
    // H<delimiter>HeaderName=HeaderValue<delimiter>
    // Q<delimiter>QueryName=QueryValue1<subdelimiter>QueryValue2<delimiter>
    // R<delimiter>RouteName1=RouteValue1<delimiter>RouteName2=RouteValue2
    // V<delimiter>ValueName1=Value1<delimiter>ValueName2=Value2
    public string CreateStorageKey(OutputCacheContext context)
    {
        ArgumentNullException.ThrowIfNull(_builderPool);

        var builder = _builderPool.Get();

        try
        {
            if (!string.IsNullOrEmpty(context.CacheVaryByRules.CacheKeyPrefix))
            {
                builder
                    .Append(context.CacheVaryByRules.CacheKeyPrefix)
                    .Append(KeyDelimiter);
            }

            AppendBaseKey(context, builder);

            AppendVaryByKey(context, builder);

            return builder.ToString();
        }
        finally
        {
            _builderPool.Return(builder);
        }
    }

    // GET<delimiter>SCHEME<delimiter>HOST:PORT/PATHBASE/PATH
    public void AppendBaseKey(OutputCacheContext context, StringBuilder builder)
    {
        var request = context.HttpContext.Request;

        builder
            .AppendUpperInvariant(request.Method)
            .Append(KeyDelimiter)
            .AppendUpperInvariant(request.Scheme)
            .Append(KeyDelimiter)
            .AppendUpperInvariant(request.Host.Value);

        if (_options.UseCaseSensitivePaths)
        {
            builder
                .Append(request.PathBase.Value)
                .Append(request.Path.Value);
        }
        else
        {
            builder
                .AppendUpperInvariant(request.PathBase.Value)
                .AppendUpperInvariant(request.Path.Value);
        }
    }

    public void AppendVaryByKey(OutputCacheContext context, StringBuilder builder)
    {
        var varyByRules = context.CacheVaryByRules;

        if (varyByRules == null)
        {
            throw new InvalidOperationException($"{nameof(OutputCacheContext.CacheVaryByRules)} must not be null on the {nameof(OutputCacheContext)}");
        }

        var varyHeaderNames = context.CacheVaryByRules.HeaderNames;
        var varyRouteValueNames = context.CacheVaryByRules.RouteValueNames;
        var varyQueryKeys = context.CacheVaryByRules.QueryKeys;
        var varyByValues = context.CacheVaryByRules.HasVaryByValues ? context.CacheVaryByRules.VaryByValues : null;

        // Vary by header names
        var headersCount = varyByRules.HeaderNames.Count;

        if (headersCount > 0)
        {
            // Append a group separator for the header segment of the cache key
            builder
                .Append(KeyDelimiter)
                .Append('H');

            var requestHeaders = context.HttpContext.Request.Headers;
            for (var i = 0; i < headersCount; i++)
            {
                var header = varyByRules.HeaderNames[i] ?? string.Empty;
                var headerValues = requestHeaders[header];
                builder
                    .Append(KeyDelimiter)
                    .Append(header)
                    .Append('=');

                var headerValuesArray = headerValues.ToArray();
                Array.Sort(headerValuesArray, StringComparer.Ordinal);

                for (var j = 0; j < headerValuesArray.Length; j++)
                {
                    builder.Append(headerValuesArray[j]);
                }
            }
        }

        // Vary by query keys
        if (varyQueryKeys.Count > 0)
        {
            // Append a group separator for the query key segment of the cache key
            builder
                .Append(KeyDelimiter)
                .Append('Q');

            if (varyQueryKeys.Count == 1 && string.Equals(varyQueryKeys[0], "*", StringComparison.Ordinal) && context.HttpContext.Request.Query.Count > 0)
            {
                // Vary by all available query keys
                var queryArray = context.HttpContext.Request.Query.ToArray();
                // Query keys are aggregated case-insensitively whereas the query values are compared ordinally.
                Array.Sort(queryArray, QueryKeyComparer.OrdinalIgnoreCase);

                for (var i = 0; i < queryArray.Length; i++)
                {
                    builder
                        .Append(KeyDelimiter)
                        .AppendUpperInvariant(queryArray[i].Key)
                        .Append('=');

                    var queryValueArray = queryArray[i].Value.ToArray();
                    Array.Sort(queryValueArray, StringComparer.Ordinal);

                    for (var j = 0; j < queryValueArray.Length; j++)
                    {
                        if (j > 0)
                        {
                            builder.Append(KeySubDelimiter);
                        }

                        builder.Append(queryValueArray[j]);
                    }
                }
            }
            else
            {
                for (var i = 0; i < varyByRules.QueryKeys.Count; i++)
                {
                    var queryKey = varyByRules.QueryKeys[i] ?? string.Empty;
                    var queryKeyValues = context.HttpContext.Request.Query[queryKey];
                    builder
                        .Append(KeyDelimiter)
                        .Append(queryKey)
                        .Append('=');

                    var queryValueArray = queryKeyValues.ToArray();
                    Array.Sort(queryValueArray, StringComparer.Ordinal);

                    for (var j = 0; j < queryValueArray.Length; j++)
                    {
                        if (j > 0)
                        {
                            builder.Append(KeySubDelimiter);
                        }

                        builder.Append(queryValueArray[j]);
                    }
                }
            }
        }

        // Vary by route value names
        var routeValueNamesCount = varyByRules.RouteValueNames.Count;
        if (routeValueNamesCount > 0)
        {
            // Append a group separator for the route values segment of the cache key
            builder
                .Append(KeyDelimiter)
                .Append('R');

            for (var i = 0; i < routeValueNamesCount; i++)
            {
                // The lookup key can't be null
                var routeValueName = varyByRules.RouteValueNames[i] ?? string.Empty;

                // RouteValueNames returns null if the key doesn't exist
                var routeValueValue = context.HttpContext.Request.RouteValues[routeValueName];

                builder.Append(KeyDelimiter)
                    .Append(routeValueName)
                    .Append('=')
                    .Append(Convert.ToString(routeValueValue, CultureInfo.InvariantCulture));
            }
        }

        // Vary by values

        // Order keys to have a deterministic key
        var orderedKeys = GetOrderDictionaryKeys(varyByValues);

        var valueNamesCount = orderedKeys.Length;
        if (valueNamesCount > 0)
        {
            // Append a group separator for the values segment of the cache key
            builder
                .Append(KeyDelimiter)
                .Append('V');

            for (var i = 0; i < valueNamesCount; i++)
            {
                // The lookup key can't be null
                var key = orderedKeys[i] ?? string.Empty;

                var value = varyByRules.VaryByValues[key];

                builder.Append(KeyDelimiter)
                    .Append(key)
                    .Append('=')
                    .Append(value);
            }
        }
    }

    internal static string[] GetOrderDictionaryKeys(IDictionary<string, string>? dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return Array.Empty<string>();
        }

        var newArray = dictionary.Keys.ToArray();

        Array.Sort(newArray, StringComparer.OrdinalIgnoreCase);

        return newArray;
    }

    private sealed class QueryKeyComparer : IComparer<KeyValuePair<string, StringValues>>
    {
        private readonly StringComparer _stringComparer;

        public static QueryKeyComparer OrdinalIgnoreCase { get; } = new QueryKeyComparer(StringComparer.OrdinalIgnoreCase);

        public QueryKeyComparer(StringComparer stringComparer)
        {
            _stringComparer = stringComparer;
        }

        public int Compare(KeyValuePair<string, StringValues> x, KeyValuePair<string, StringValues> y) => _stringComparer.Compare(x.Key, y.Key);
    }
}
