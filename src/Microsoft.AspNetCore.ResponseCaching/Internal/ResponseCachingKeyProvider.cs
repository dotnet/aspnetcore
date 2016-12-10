// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public class ResponseCachingKeyProvider : IResponseCachingKeyProvider
    {
        // Use the record separator for delimiting components of the cache key to avoid possible collisions
        private static readonly char KeyDelimiter = '\x1e';

        private readonly ObjectPool<StringBuilder> _builderPool;
        private readonly ResponseCachingOptions _options;

        public ResponseCachingKeyProvider(ObjectPoolProvider poolProvider, IOptions<ResponseCachingOptions> options)
        {
            if (poolProvider == null)
            {
                throw new ArgumentNullException(nameof(poolProvider));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _builderPool = poolProvider.CreateStringBuilderPool();
            _options = options.Value;
        }

        public IEnumerable<string> CreateLookupVaryByKeys(ResponseCachingContext context)
        {
            return new string[] { CreateStorageVaryByKey(context) };
        }

        // GET<delimiter>/PATH
        public string CreateBaseKey(ResponseCachingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;
            var builder = _builderPool.Get();

            try
            {
                builder
                    .AppendUpperInvariant(request.Method)
                    .Append(KeyDelimiter);

                if (_options.UseCaseSensitivePaths)
                {
                    builder.Append(request.Path.Value);
                }
                else
                {
                    builder.AppendUpperInvariant(request.Path.Value);
                }

                return builder.ToString();
            }
            finally
            {
                _builderPool.Return(builder);
            }
        }

        // BaseKey<delimiter>H<delimiter>HeaderName=HeaderValue<delimiter>Q<delimiter>QueryName=QueryValue
        public string CreateStorageVaryByKey(ResponseCachingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var varyByRules = context.CachedVaryByRules;
            if (varyByRules == null)
            {
                throw new InvalidOperationException($"{nameof(CachedVaryByRules)} must not be null on the {nameof(ResponseCachingContext)}");
            }

            if ((StringValues.IsNullOrEmpty(varyByRules.Headers) && StringValues.IsNullOrEmpty(varyByRules.QueryKeys)))
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
                if (varyByRules?.Headers.Count > 0)
                {
                    // Append a group separator for the header segment of the cache key
                    builder.Append(KeyDelimiter)
                        .Append('H');

                    for (var i = 0; i < varyByRules.Headers.Count; i++)
                    {
                        var header = varyByRules.Headers[i];
                        var headerValues = context.HttpContext.Request.Headers[header];
                        builder.Append(KeyDelimiter)
                            .Append(header)
                            .Append("=");

                        for (var j = 0; j < headerValues.Count; j++)
                        {
                            builder.Append(headerValues[j]);
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
                        foreach (var query in context.HttpContext.Request.Query.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            builder.Append(KeyDelimiter)
                                .AppendUpperInvariant(query.Key)
                                .Append("=");

                            for (var i = 0; i < query.Value.Count; i++)
                            {
                                builder.Append(query.Value[i]);
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < varyByRules.QueryKeys.Count; i++)
                        {
                            var queryKey = varyByRules.QueryKeys[i];
                            var queryKeyValues = context.HttpContext.Request.Query[queryKey];
                            builder.Append(KeyDelimiter)
                                .Append(queryKey)
                                .Append("=");

                            for (var j = 0; j < queryKeyValues.Count; j++)
                            {
                                builder.Append(queryKeyValues[j]);
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
    }
}
