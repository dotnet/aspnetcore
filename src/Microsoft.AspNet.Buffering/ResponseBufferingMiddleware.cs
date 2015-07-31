// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Buffering
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

            var originalBufferingFeature = httpContext.GetFeature<IHttpBufferingFeature>();
            var originalSendFileFeature = httpContext.GetFeature<IHttpSendFileFeature>();
            try
            {
                // Shim the response stream
                var bufferStream = new BufferingWriteStream(originalResponseBody);
                httpContext.Response.Body = bufferStream;
                httpContext.SetFeature<IHttpBufferingFeature>(new HttpBufferingFeature(bufferStream, originalBufferingFeature));
                if (originalSendFileFeature != null)
                {
                    httpContext.SetFeature<IHttpSendFileFeature>(new SendFileFeatureWrapper(originalSendFileFeature, bufferStream));
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
                httpContext.SetFeature(originalBufferingFeature);
                httpContext.SetFeature(originalSendFileFeature);
                httpContext.Response.Body = originalResponseBody;
            }
        }
    }
}
