// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Enable HTTP response compression.
    /// </summary>
    public class ResponseCompressionMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly IResponseCompressionProvider _provider;


        /// <summary>
        /// Initialize the Response Compression middleware.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="provider"></param>
        public ResponseCompressionMiddleware(RequestDelegate next, IResponseCompressionProvider provider)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _next = next;
            _provider = provider;
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (!_provider.CheckRequestAcceptsCompression(context))
            {
                await _next(context);
                return;
            }

            var bodyStream = context.Response.Body;
            var originalBufferFeature = context.Features.Get<IHttpBufferingFeature>();
            var originalSendFileFeature = context.Features.Get<IHttpSendFileFeature>();

            var bodyWrapperStream = new BodyWrapperStream(context, bodyStream, _provider,
                originalBufferFeature, originalSendFileFeature);
            context.Response.Body = bodyWrapperStream;
            context.Features.Set<IHttpBufferingFeature>(bodyWrapperStream);
            if (originalSendFileFeature != null)
            {
                context.Features.Set<IHttpSendFileFeature>(bodyWrapperStream);
            }

            try
            {
                await _next(context);
                await bodyWrapperStream.FinishCompressionAsync();
            }
            finally
            {
                context.Response.Body = bodyStream;
                context.Features.Set(originalBufferFeature);
                if (originalSendFileFeature != null)
                {
                    context.Features.Set(originalSendFileFeature);
                }
            }
        }
    }
}
