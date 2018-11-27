// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports
{
    public class ServerSentEventsTransport : IHttpTransport
    {
        private readonly PipeReader _application;
        private readonly string _connectionId;
        private readonly ILogger _logger;

        public ServerSentEventsTransport(PipeReader application, string connectionId, ILoggerFactory loggerFactory)
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
                while (true)
                {
                    var result = await _application.ReadAsync(token);
                    var buffer = result.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            Log.SSEWritingMessage(_logger, buffer.Length);

                            await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, context.Response.Body);
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _application.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Closed connection
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, long, Exception> _sseWritingMessage =
                LoggerMessage.Define<long>(LogLevel.Trace, new EventId(1, "SSEWritingMessage"), "Writing a {Count} byte message.");

            public static void SSEWritingMessage(ILogger logger, long count)
            {
                _sseWritingMessage(logger, count, null);
            }
        }
    }
}
