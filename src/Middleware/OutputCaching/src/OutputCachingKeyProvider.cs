// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCachingKeyProvider : IOutputCachingKeyProvider
{
    // Use the record separator for delimiting components of the cache key to avoid possible collisions
    private const char KeyDelimiter = '\x1e';
    // Use the unit separator for delimiting subcomponents of the cache key to avoid possible collisions
    private const char KeySubDelimiter = '\x1f';

    private readonly ObjectPool<StringBuilder> _builderPool;
    private readonly OutputCachingOptions _options;

    internal OutputCachingKeyProvider(ObjectPoolProvider poolProvider, IOptions<OutputCachingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(poolProvider, nameof(poolProvider));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        _builderPool = poolProvider.CreateStringBuilderPool();
        _options = options.Value;
    }

    // GET<delimiter>SCHEME<delimiter>HOST:PORT/PATHBASE/PATH<delimiter>H<delimiter>HeaderName=HeaderValue<delimiter>Q<delimiter>QueryName=QueryValue1<subdelimiter>QueryValue2
    public string CreateStorageVaryByKey(OutputCachingContext context)
    {
        ArgumentNullException.ThrowIfNull(_builderPool, nameof(context));

        var varyByRules = context.CachedVaryByRules;
        if (varyByRules == null)
        {
            throw new InvalidOperationException($"{nameof(CachedVaryByRules)} must not be null on the {nameof(OutputCachingContext)}");
        }

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

            // Vary by prefix and custom
            var prefixCount = varyByRules?.VaryByPrefix.Count ?? 0;
            if (prefixCount > 0)
            {
                // Append a group separator for the header segment of the cache key
                builder.Append(KeyDelimiter)
                    .Append('C');

                for (var i = 0; i < prefixCount; i++)
                {
                    var value = varyByRules?.VaryByPrefix[i] ?? string.Empty;
                    builder.Append(KeyDelimiter).Append(value);
                }
            }

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

    private class QueryKeyComparer : IComparer<KeyValuePair<string, StringValues>>
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
