// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
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

    private static readonly char[] KeyDelimiters = new[] { KeyDelimiter, KeySubDelimiter };

    private readonly ObjectPool<StringBuilder> _builderPool;
    private readonly OutputCacheOptions _options;

    internal OutputCacheKeyProvider(ObjectPoolProvider poolProvider, IOptions<OutputCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(poolProvider);
        ArgumentNullException.ThrowIfNull(options);

        _builderPool = poolProvider.CreateStringBuilderPool();
        _options = options.Value;
    }

    // <VaryByKeyPrefix><delimiter>GET<delimiter>SCHEME<delimiter>HOST:PORT/PATHBASE/PATH<delimiter>H<delimiter>HeaderName=HeaderValue<delimiter>Q<delimiter>QueryName=QueryValue1<subdelimiter>QueryValue2<delimiter>R<delimiter>RouteName1=RouteValue1<subdelimiter>RouteName2=RouteValue2
    public string CreateStorageKey(OutputCacheContext context)
    {
        ArgumentNullException.ThrowIfNull(_builderPool);

        var builder = _builderPool.Get();

        try
        {
            if (!TryAppendKeyPrefix(context, builder))
            {
                return string.Empty;
            }

            if (!TryAppendBaseKey(context, builder))
            {
                return string.Empty;
            }

            if (!TryAppendVaryByKey(context, builder))
            {
                return string.Empty;
            }

            return builder.ToString();
        }
        finally
        {
            _builderPool.Return(builder);
        }
    }

    public static bool ContainsDelimiters(string? value)
    {
        return !string.IsNullOrEmpty(value) && value.IndexOfAny(KeyDelimiters) > -1;
    }

    public static bool TryAppendKeyPrefix(OutputCacheContext context, StringBuilder builder)
    {
        var cacheKeyPrefix = context.CacheVaryByRules.CacheKeyPrefix;

        if (!string.IsNullOrEmpty(cacheKeyPrefix))
        {
            if (ContainsDelimiters(cacheKeyPrefix))
            {
                return false;
            }

            builder
                .Append(context.CacheVaryByRules.CacheKeyPrefix)
                .Append(KeyDelimiter);
        }

        return true;
    }

    // GET<delimiter>SCHEME<delimiter>HOST:PORT/PATHBASE/PATH
    public bool TryAppendBaseKey(OutputCacheContext context, StringBuilder builder)
    {
        var request = context.HttpContext.Request;

        if (ContainsDelimiters(request.Method) ||
            ContainsDelimiters(request.Scheme) ||
            ContainsDelimiters(request.PathBase.Value) ||
            ContainsDelimiters(request.Path.Value))
        {
            return false;
        }

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

        return true;
    }

    public bool TryAppendVaryByKey(OutputCacheContext context, StringBuilder builder)
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

        // Normalize order and casing of vary by rules
        var normalizedVaryHeaderNames = GetOrderCasingNormalizedStringValues(varyHeaderNames);
        var normalizedVaryRouteValueNames = GetOrderCasingNormalizedStringValues(varyRouteValueNames);
        var normalizedVaryQueryKeys = GetOrderCasingNormalizedStringValues(varyQueryKeys);
        var normalizedVaryByValues = GetOrderDictionary(varyByValues);

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

                if (ContainsDelimiters(header))
                {
                    return false;
                }

                builder
                    .Append(KeyDelimiter)
                    .Append(header)
                    .Append('=');

                var headerValuesArray = headerValues.ToArray();
                Array.Sort(headerValuesArray, StringComparer.Ordinal);

                for (var j = 0; j < headerValuesArray.Length; j++)
                {
                    if (ContainsDelimiters(headerValuesArray[j]))
                    {
                        return false;
                    }

                    builder.Append(headerValuesArray[j]);
                }
            }
        }

        // Vary by query keys
        if (normalizedVaryQueryKeys.Length > 0)
        {
            // Append a group separator for the query key segment of the cache key
            builder
                .Append(KeyDelimiter)
                .Append('Q');

            if (normalizedVaryQueryKeys.Length == 1 && string.Equals(normalizedVaryQueryKeys[0], "*", StringComparison.Ordinal) && context.HttpContext.Request.Query.Count > 0)
            {
                // Vary by all available query keys
                var queryArray = context.HttpContext.Request.Query.ToArray();
                // Query keys are aggregated case-insensitively whereas the query values are compared ordinally.
                Array.Sort(queryArray, QueryKeyComparer.OrdinalIgnoreCase);

                for (var i = 0; i < queryArray.Length; i++)
                {
                    if (ContainsDelimiters(queryArray[i].Key))
                    {
                        return false;
                    }

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

                        if (ContainsDelimiters(queryValueArray[j]))
                        {
                            return false;
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

                    if (ContainsDelimiters(queryKey))
                    {
                        return false;
                    }

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

                        if (ContainsDelimiters(queryValueArray[j]))
                        {
                            return false;
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
                var stringRouteValue = Convert.ToString(routeValueValue, CultureInfo.InvariantCulture);

                if (ContainsDelimiters(routeValueName) ||
                    ContainsDelimiters(stringRouteValue))
                {
                    return false;
                }

                builder.Append(KeyDelimiter)
                    .Append(routeValueName)
                    .Append('=')
                    .Append(stringRouteValue);
            }
        }

        // Vary by values
        var valueNamesCount = normalizedVaryByValues.Length;
        if (valueNamesCount > 0)
        {
            // Append a group separator for the values segment of the cache key
            builder
                .Append(KeyDelimiter)
                .Append('V');

            for (var i = 0; i < valueNamesCount; i++)
            {
                // The lookup key can't be null
                var key = normalizedVaryByValues[i] ?? string.Empty;

                var value = varyByRules.VaryByValues[key];

                if (ContainsDelimiters(key) ||
                    ContainsDelimiters(value))
                {
                    return false;
                }

                builder.Append(KeyDelimiter)
                    .Append(key)
                    .Append('=')
                    .Append(value);
            }
        }

        return true;
    }

    // Normalize order and casing
    internal static string[] GetOrderCasingNormalizedStringValues(StringValues stringValues)
    {
        if (stringValues.Count == 0)
        {
            return Array.Empty<string>();
        }
        else if (stringValues.Count == 1)
        {
            return new string[] { stringValues.ToString().ToUpperInvariant() };
        }
        else
        {
            var originalArray = stringValues.ToArray();
            var newArray = new string[originalArray.Length];

            for (var i = 0; i < originalArray.Length; i++)
            {
                newArray[i] = originalArray[i]!.ToUpperInvariant();
            }

            // Since the casing has already been normalized, use Ordinal comparison
            Array.Sort(newArray, StringComparer.OrdinalIgnoreCase);

            return newArray;
        }
    }

    internal static string[] GetOrderDictionary(IDictionary<string, string>? dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return Array.Empty<string>();
        }

        var newArray = dictionary.Keys.ToArray();

        // Since the casing has already been normalized, use Ordinal comparison
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
