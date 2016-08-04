// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Enable HTTP response compression.
    /// </summary>
    public class ResponseCompressionMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly Dictionary<string, IResponseCompressionProvider> _compressionProviders;

        private readonly Func<HttpContext, bool> _shouldCompressResponse;

        private readonly bool _enableHttps;

        /// <summary>
        /// Initialize the Response Compression middleware.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public ResponseCompressionMiddleware(RequestDelegate next, IOptions<ResponseCompressionOptions> options)
        {
            if (options.Value.ShouldCompressResponse == null)
            {
                throw new ArgumentException($"{nameof(options.Value.ShouldCompressResponse)} is not provided in argument {nameof(options)}");
            }
            _shouldCompressResponse = options.Value.ShouldCompressResponse;

            _next = next;

            var providers = options.Value.Providers;
            if (providers == null)
            {
                providers = new IResponseCompressionProvider[]
                {
                    new GzipResponseCompressionProvider(CompressionLevel.Fastest)
                };
            }
            else if (!providers.Any())
            {
                throw new ArgumentException($"{nameof(options.Value.Providers)} cannot be empty in argument {nameof(options)}");
            }

            _compressionProviders = providers.ToDictionary(p => p.EncodingName, StringComparer.OrdinalIgnoreCase);
            _compressionProviders.Add("*", providers.First());
            _compressionProviders.Add("identity", null);

            _enableHttps = options.Value.EnableHttps;
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            IResponseCompressionProvider compressionProvider = null;

            if (!context.Request.IsHttps || _enableHttps)
            {
                compressionProvider = SelectProvider(context.Request.Headers[HeaderNames.AcceptEncoding]);
            }

            if (compressionProvider == null)
            {
                await _next(context);
                return;
            }

            var bodyStream = context.Response.Body;

            using (var bodyWrapperStream = new BodyWrapperStream(context.Response, bodyStream, _shouldCompressResponse, compressionProvider))
            {
                context.Response.Body = bodyWrapperStream;

                try
                {
                    await _next(context);
                }
                finally
                {
                    context.Response.Body = bodyStream;
                }
            }
        }

        private IResponseCompressionProvider SelectProvider(StringValues acceptEncoding)
        {
            IList<StringWithQualityHeaderValue> unsorted;

            if (StringWithQualityHeaderValue.TryParseList(acceptEncoding, out unsorted) && unsorted != null)
            {
                var sorted = unsorted
                    .Where(s => s.Quality.GetValueOrDefault(1) > 0)
                    .OrderByDescending(s => s.Quality.GetValueOrDefault(1));

                foreach (var encoding in sorted)
                {
                    IResponseCompressionProvider provider;

                    if (_compressionProviders.TryGetValue(encoding.Value, out provider))
                    {
                        return provider;
                    }
                }
            }

            return null;
        }
    }
}
