// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class KeyProvider : IKeyProvider
    {
        // Use the record separator for delimiting components of the cache key to avoid possible collisions
        private static readonly char KeyDelimiter = '\x1e';

        private readonly ObjectPool<StringBuilder> _builderPool;
        private readonly ResponseCachingOptions _options;

        public KeyProvider(ObjectPoolProvider poolProvider, IOptions<ResponseCachingOptions> options)
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

        // GET<delimiter>/PATH
        // TODO: Method invariant retrieval? E.g. HEAD after GET to the same resource.
        public virtual string CreateBaseKey(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var request = httpContext.Request;
            var builder = _builderPool.Get();

            try
            {
                builder
                    .Append(request.Method.ToUpperInvariant())
                    .Append(KeyDelimiter)
                    .Append(_options.CaseSensitivePaths ? request.Path.Value : request.Path.Value.ToUpperInvariant());

                return builder.ToString();;
            }
            finally
            {
                _builderPool.Return(builder);
            }
        }

        // BaseKey<delimiter>H<delimiter>HeaderName=HeaderValue<delimiter>Q<delimiter>QueryName=QueryValue
        public virtual string CreateVaryKey(HttpContext httpContext, VaryRules varyRules)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }
            if (varyRules == null || (StringValues.IsNullOrEmpty(varyRules.Headers) && StringValues.IsNullOrEmpty(varyRules.Params)))
            {
                return httpContext.GetResponseCachingState().CachedVaryRules.VaryKeyPrefix;
            }

            var request = httpContext.Request;
            var builder = _builderPool.Get();

            try
            {
                // Prepend with the Guid of the CachedVaryRules
                builder.Append(httpContext.GetResponseCachingState().CachedVaryRules.VaryKeyPrefix);

                // Vary by headers
                if (varyRules?.Headers.Count > 0)
                {
                    // Append a group separator for the header segment of the cache key
                    builder.Append(KeyDelimiter)
                        .Append('H');

                    foreach (var header in varyRules.Headers)
                    {
                        var value = httpContext.Request.Headers[header];

                        // TODO: How to handle null/empty string?
                        if (StringValues.IsNullOrEmpty(value))
                        {
                            value = "null";
                        }

                        builder.Append(KeyDelimiter)
                            .Append(header)
                            .Append("=")
                            .Append(value);
                    }
                }

                // Vary by query params
                if (varyRules?.Params.Count > 0)
                {
                    // Append a group separator for the query parameter segment of the cache key
                    builder.Append(KeyDelimiter)
                        .Append('Q');

                    if (varyRules.Params.Count == 1 && string.Equals(varyRules.Params[0], "*", StringComparison.Ordinal))
                    {
                        // Vary by all available query params
                        foreach (var query in httpContext.Request.Query.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            builder.Append(KeyDelimiter)
                                .Append(query.Key.ToUpperInvariant())
                                .Append("=")
                                .Append(query.Value);
                        }
                    }
                    else
                    {
                        foreach (var param in varyRules.Params)
                        {
                            var value = httpContext.Request.Query[param];

                            // TODO: How to handle null/empty string?
                            if (StringValues.IsNullOrEmpty(value))
                            {
                                value = "null";
                            }

                            builder.Append(KeyDelimiter)
                                .Append(param)
                                .Append("=")
                                .Append(value);
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
