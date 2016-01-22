// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Buffering
{
    public class ResponseBufferingMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseBufferingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var originalResponseBody = httpContext.Response.Body;

            // no-op if buffering is already available.
            if (originalResponseBody.CanSeek)
            {
                await _next(httpContext);
                return;
            }

            var originalBufferingFeature = httpContext.Features.Get<IHttpBufferingFeature>();
            var originalSendFileFeature = httpContext.Features.Get<IHttpSendFileFeature>();
            try
            {
                // Shim the response stream
                var bufferStream = new BufferingWriteStream(originalResponseBody);
                httpContext.Response.Body = bufferStream;
                httpContext.Features.Set<IHttpBufferingFeature>(new HttpBufferingFeature(bufferStream, originalBufferingFeature));
                if (originalSendFileFeature != null)
                {
                    httpContext.Features.Set<IHttpSendFileFeature>(new SendFileFeatureWrapper(originalSendFileFeature, bufferStream));
                }

                await _next(httpContext);

                // If we're still buffered, set the content-length header and flush the buffer.
                // Only if the content-length header is not already set, and some content was buffered.
                if (!httpContext.Response.HasStarted && bufferStream.CanSeek && bufferStream.Length > 0)
                {
                    if (!httpContext.Response.ContentLength.HasValue)
                    {
                        httpContext.Response.ContentLength = bufferStream.Length;
                    }
                    await bufferStream.FlushAsync();
                }
            }
            finally
            {
                // undo everything
                httpContext.Features.Set(originalBufferingFeature);
                httpContext.Features.Set(originalSendFileFeature);
                httpContext.Response.Body = originalResponseBody;
            }
        }
    }
}
