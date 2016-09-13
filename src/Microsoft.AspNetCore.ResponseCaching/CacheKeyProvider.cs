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

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class CacheKeyProvider : ICacheKeyProvider
    {
        // Use the record separator for delimiting components of the cache key to avoid possible collisions
        private static readonly char KeyDelimiter = '\x1e';

        private readonly ObjectPool<StringBuilder> _builderPool;
        private readonly ResponseCachingOptions _options;

        public CacheKeyProvider(ObjectPoolProvider poolProvider, IOptions<ResponseCachingOptions> options)
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

        public virtual IEnumerable<string> CreateLookupBaseKeys(ResponseCachingContext context)
        {
            return new string[] { CreateStorageBaseKey(context) };
        }

        public virtual IEnumerable<string> CreateLookupVaryKeys(ResponseCachingContext context)
        {
            return new string[] { CreateStorageVaryKey(context) };
        }

        // GET<delimiter>/PATH
        public virtual string CreateStorageBaseKey(ResponseCachingContext context)
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
                    .Append(_options.CaseSensitivePaths ? request.Path.Value : request.Path.Value.ToUpperInvariant());

                return builder.ToString();;
            }
            finally
            {
                _builderPool.Return(builder);
            }
        }

        // BaseKey<delimiter>H<delimiter>HeaderName=HeaderValue<delimiter>Q<delimiter>QueryName=QueryValue
        public virtual string CreateStorageVaryKey(ResponseCachingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var varyRules = context.CachedVaryRules;
            if  (varyRules == null)
            {
                throw new InvalidOperationException($"{nameof(CachedVaryRules)} must not be null on the {nameof(ResponseCachingContext)}");
            }

            if ((StringValues.IsNullOrEmpty(varyRules.Headers) && StringValues.IsNullOrEmpty(varyRules.Params)))
            {
                return varyRules.VaryKeyPrefix;
            }

            var request = context.HttpContext.Request;
            var builder = _builderPool.Get();

            try
            {
                // Prepend with the Guid of the CachedVaryRules
                builder.Append(varyRules.VaryKeyPrefix);

                // Vary by headers
                if (varyRules?.Headers.Count > 0)
                {
                    // Append a group separator for the header segment of the cache key
                    builder.Append(KeyDelimiter)
                        .Append('H');

                    foreach (var header in varyRules.Headers)
                    {
                        builder.Append(KeyDelimiter)
                            .Append(header)
                            .Append("=")
                            // TODO: Perf - iterate the string values instead?
                            .Append(context.HttpContext.Request.Headers[header]);
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
                        foreach (var param in varyRules.Params)
                        {
                            builder.Append(KeyDelimiter)
                                .Append(param)
                                .Append("=")
                                // TODO: Perf - iterate the string values instead?
                                .Append(context.HttpContext.Request.Query[param]);
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
