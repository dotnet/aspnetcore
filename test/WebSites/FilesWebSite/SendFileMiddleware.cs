// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;

namespace FilesWebSite
{
    public class SendFileMiddleware
    {
        private const int DefaultBufferSize = 0x1000;

        private readonly RequestDelegate _next;

        public SendFileMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var environment = (IApplicationEnvironment)context.RequestServices.GetService(typeof(IApplicationEnvironment));

            if (context.GetFeature<IHttpSendFileFeature>() == null)
            {
                var sendFile = new SendFileFallBack(context.Response.Body, environment.ApplicationBasePath);
                context.SetFeature<IHttpSendFileFeature>(sendFile);
            }

            await _next(context);
        }

        private class SendFileFallBack : IHttpSendFileFeature
        {
            private readonly string _appBasePath;
            private Stream _responseStream;

            public SendFileFallBack(Stream responseStream, string appBasePath)
            {
                _responseStream = responseStream;
                _appBasePath = appBasePath;
            }

            public async Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
            {
                using (var stream = new FileStream(Path.Combine(_appBasePath, path), FileMode.Open))
                {
                    length = length ?? stream.Length - offset;

                    stream.Seek(offset, SeekOrigin.Begin);

                    var bufferSize = length < DefaultBufferSize ? length.Value : DefaultBufferSize;
                    var buffer = new byte[bufferSize];
                    var bytesRead = 0;

                    do
                    {
                        var bytesToRead = bufferSize < length ? bufferSize : length;
                        bytesRead = await stream.ReadAsync(buffer, 0, (int)bytesToRead);
                        length = length - bytesRead;

                        await _responseStream.WriteAsync(buffer, 0, bytesRead);
                    } while (bytesRead > 0 && length > 0);
                }
            }
        }
    }
}