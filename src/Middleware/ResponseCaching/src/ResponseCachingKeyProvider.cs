// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching;

internal sealed class ResponseCachingKeyProvider : IResponseCachingKeyProvider
{
    // Use the record separator for delimiting components of the cache key to avoid possible collisions
    private const char KeyDelimiter = '\x1e';
    // Use the unit separator for delimiting subcomponents of the cache key to avoid possible collisions
    private const char KeySubDelimiter = '\x1f';

    private readonly ObjectPool<StringBuilder> _builderPool;
    private readonly ResponseCachingOptions _options;

    internal ResponseCachingKeyProvider(ObjectPoolProvider poolProvider, IOptions<ResponseCachingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(poolProvider);
        ArgumentNullException.ThrowIfNull(options);

        _builderPool = poolProvider.CreateStringBuilderPool();
        _options = options.Value;
    }

    public IEnumerable<string> CreateLookupVaryByKeys(ResponseCachingContext context)
    {
        return new string[] { CreateStorageVaryByKey(context) };
    }

    // GET<delimiter>SCHEME<delimiter>HOST:PORT/PATHBASE/PATH
    public string CreateBaseKey(ResponseCachingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.HttpContext.Request;
        var builder = _builderPool.Get();

        try
        {
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

            return builder.ToString();
        }
        finally
        {
            _builderPool.Return(builder);
        }
    }

    // BaseKey<delimiter>H<delimiter>HeaderName=HeaderValue<delimiter>Q<delimiter>QueryName=QueryValue1<subdelimiter>QueryValue2
    public string CreateStorageVaryByKey(ResponseCachingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var varyByRules = context.CachedVaryByRules;
        if (varyByRules == null)
        {
            throw new InvalidOperationException($"{nameof(CachedVaryByRules)} must not be null on the {nameof(ResponseCachingContext)}");
        }

        if (StringValues.IsNullOrEmpty(varyByRules.Headers) && StringValues.IsNullOrEmpty(varyByRules.QueryKeys))
        {
            return varyByRules.VaryByKeyPrefix;
        }

        var request = context.HttpContext.Request;
        var builder = _builderPool.Get();

        try
        {
            // Prepend with the Guid of the CachedVaryByRules
            builder.Append(varyByRules.VaryByKeyPrefix);

            // Vary by headers
            var headersCount = varyByRules?.Headers.Count ?? 0;
            if (headersCount > 0)
            {
                // Append a group separator for the header segment of the cache key
                builder.Append(KeyDelimiter)
                    .Append('H');

                var requestHeaders = context.HttpContext.Request.Headers;
                for (var i = 0; i < headersCount; i++)
                {
                    var header = varyByRules!.Headers[i] ?? string.Empty;
                    var headerValues = requestHeaders[header];
                    builder.Append(KeyDelimiter)
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
            if (varyByRules?.QueryKeys.Count > 0)
            {
                // Append a group separator for the query key segment of the cache key
                builder.Append(KeyDelimiter)
                    .Append('Q');

                if (varyByRules.QueryKeys.Count == 1 && string.Equals(varyByRules.QueryKeys[0], "*", StringComparison.Ordinal))
                {
                    // Vary by all available query keys
                    var queryArray = context.HttpContext.Request.Query.ToArray();
                    // Query keys are aggregated case-insensitively whereas the query values are compared ordinally.
                    Array.Sort(queryArray, QueryKeyComparer.OrdinalIgnoreCase);

                    for (var i = 0; i < queryArray.Length; i++)
                    {
                        builder.Append(KeyDelimiter)
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
                        builder.Append(KeyDelimiter)
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

            return builder.ToString();
        }
        finally
        {
            _builderPool.Return(builder);
        }
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
