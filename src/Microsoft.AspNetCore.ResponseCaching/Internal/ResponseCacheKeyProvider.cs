// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public class ResponseCacheKeyProvider : IResponseCacheKeyProvider
    {
        // Use the record separator for delimiting components of the cache key to avoid possible collisions
        private static readonly char KeyDelimiter = '\x1e';

        private readonly ObjectPool<StringBuilder> _builderPool;
        private readonly ResponseCacheOptions _options;

        public ResponseCacheKeyProvider(ObjectPoolProvider poolProvider, IOptions<ResponseCacheOptions> options)
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

        public IEnumerable<string> CreateLookupVaryByKeys(ResponseCacheContext context)
        {
            return new string[] { CreateStorageVaryByKey(context) };
        }

        // GET<delimiter>/PATH
        public string CreateBaseKey(ResponseCacheContext context)
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
                    .Append(request.Method.ToUpperInvariant())
                    .Append(KeyDelimiter)
                    .Append(_options.UseCaseSensitivePaths ? request.Path.Value : request.Path.Value.ToUpperInvariant());

                return builder.ToString();;
            }
            finally
            {
                _builderPool.Return(builder);
            }
        }

        // BaseKey<delimiter>H<delimiter>HeaderName=HeaderValue<delimiter>Q<delimiter>QueryName=QueryValue
        public string CreateStorageVaryByKey(ResponseCacheContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var varyByRules = context.CachedVaryByRules;
            if (varyByRules == null)
            {
                throw new InvalidOperationException($"{nameof(CachedVaryByRules)} must not be null on the {nameof(ResponseCacheContext)}");
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

                    foreach (var header in varyByRules.Headers)
                    {
                        builder.Append(KeyDelimiter)
                            .Append(header)
                            .Append("=")
                            // TODO: Perf - iterate the string values instead?
                            .Append(context.HttpContext.Request.Headers[header]);
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
                                .Append(query.Key.ToUpperInvariant())
                                .Append("=")
                                .Append(query.Value);
                        }
                    }
                    else
                    {
                        foreach (var queryKey in varyByRules.QueryKeys)
                        {
                            builder.Append(KeyDelimiter)
                                .Append(queryKey)
                                .Append("=")
                                // TODO: Perf - iterate the string values instead?
                                .Append(context.HttpContext.Request.Query[queryKey]);
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
