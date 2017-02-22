// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Transports
{
    public class LongPollingTransport : IHttpTransport
    {
        public static readonly string Name = "longPolling";
        private readonly ReadableChannel<Message> _application;
        private readonly ILogger _logger;

        public LongPollingTransport(ReadableChannel<Message> application, ILoggerFactory loggerFactory)
        {
            _application = application;
            _logger = loggerFactory.CreateLogger<LongPollingTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            try
            {
                if (!await _application.WaitToReadAsync(token))
                {
                    _logger.LogInformation("Terminating Long Polling connection by sending 204 response.");
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                Message message;
                if (_application.TryRead(out message))
                {
                    _logger.LogDebug("Writing {0} byte message to response", message.Payload.Length);
                    context.Response.ContentLength = message.Payload.Length;
                    await context.Response.Body.WriteAsync(message.Payload, 0, message.Payload.Length);
                }
            }
            catch (OperationCanceledException)
            {
                if (!context.RequestAborted.IsCancellationRequested)
                {
                    _logger.LogInformation("Terminating Long Polling connection by sending 204 response.");
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    throw;
                }

                // Don't count this as cancellation, this is normal as the poll can end due to the browesr closing.
                // The background thread will eventually dispose this connection if it's inactive
                _logger.LogDebug("Client disconnected from Long Polling endpoint.");
            }
        }
    }
}
