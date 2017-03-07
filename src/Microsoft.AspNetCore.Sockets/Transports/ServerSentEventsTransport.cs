// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Formatters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Transports
{
    public class ServerSentEventsTransport : IHttpTransport
    {
        public static readonly string Name = "serverSentEvents";
        private readonly ReadableChannel<Message> _application;
        private readonly ILogger _logger;

        public ServerSentEventsTransport(ReadableChannel<Message> application, ILoggerFactory loggerFactory)
        {
            _application = application;
            _logger = loggerFactory.CreateLogger<ServerSentEventsTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Content-Encoding"] = "identity";

            await context.Response.Body.FlushAsync();

            var pipe = context.Response.Body.AsPipelineWriter();

            try
            {
                while (await _application.WaitToReadAsync(token))
                {
                    var buffer = pipe.Alloc();
                    while (_application.TryRead(out var message))
                    {
                        if (!ServerSentEventsMessageFormatter.TryFormatMessage(message, buffer.Memory.Span, out var written))
                        {
                            // We need to expand the buffer
                            // REVIEW: I'm not sure I fully understand the "right" pattern here...
                            buffer.Ensure(LongPollingTransport.MaxBufferSize);

                            // Try one more time
                            if (!ServerSentEventsMessageFormatter.TryFormatMessage(message, buffer.Memory.Span, out written))
                            {
                                // Message too large
                                throw new InvalidOperationException($"Message is too large to write. Maximum allowed message size is: {LongPollingTransport.MaxBufferSize}");
                            }
                        }
                        buffer.Advance(written);
                        buffer.Commit();
                        buffer = pipe.Alloc();
                    }

                    await buffer.FlushAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Closed connection
            }
        }
    }
}
