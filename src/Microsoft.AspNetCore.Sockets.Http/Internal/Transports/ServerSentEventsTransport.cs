// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Internal.Transports
{
    public class ServerSentEventsTransport : IHttpTransport
    {
        private readonly ChannelReader<byte[]> _application;
        private readonly string _connectionId;
        private readonly ILogger _logger;

        public ServerSentEventsTransport(ChannelReader<byte[]> application, string connectionId, ILoggerFactory loggerFactory)
        {
            _application = application;
            _connectionId = connectionId;
            _logger = loggerFactory.CreateLogger<ServerSentEventsTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";

            // Make sure we disable all response buffering for SSE
            var bufferingFeature = context.Features.Get<IHttpBufferingFeature>();
            bufferingFeature?.DisableResponseBuffering();

            context.Response.Headers["Content-Encoding"] = "identity";

            // Workaround for a Firefox bug where EventSource won't fire the open event
            // until it receives some data
            await context.Response.WriteAsync(":\r\n");
            await context.Response.Body.FlushAsync();

            try
            {
                while (await _application.WaitToReadAsync(token))
                {
                    var ms = new MemoryStream();
                    while (_application.TryRead(out var buffer))
                    {
                        _logger.SSEWritingMessage(buffer.Length);

                        ServerSentEventsMessageFormatter.WriteMessage(buffer, ms);
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    await ms.CopyToAsync(context.Response.Body);
                }

                await _application.Completion;
            }
            catch (OperationCanceledException)
            {
                // Closed connection
            }
        }
    }
}
