// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Transports
{
    public class LongPollingTransport : IHttpTransport
    {
        private readonly ReadableChannel<byte[]> _application;
        private readonly ILogger _logger;
        private readonly CancellationToken _timeoutToken;

        public LongPollingTransport(CancellationToken timeoutToken, ReadableChannel<byte[]> application, ILoggerFactory loggerFactory)
        {
            _timeoutToken = timeoutToken;
            _application = application;
            _logger = loggerFactory.CreateLogger<LongPollingTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            try
            {
                if (!await _application.WaitToReadAsync(token))
                {
                    await _application.Completion;
                    _logger.LogInformation("Terminating Long Polling connection by sending 204 response.");
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                // REVIEW: What should the content type be?

                var contentLength = 0;
                var buffers = new List<byte[]>();
                // We're intentionally not checking cancellation here because we need to drain messages we've got so far,
                // but it's too late to emit the 204 required by being cancelled.
                while (_application.TryRead(out var buffer))
                {
                    contentLength += buffer.Length;
                    buffers.Add(buffer);

                    _logger.LogDebug("Writing {0} byte message to response", buffer.Length);
                }

                context.Response.ContentLength = contentLength;

                foreach (var buffer in buffers)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (OperationCanceledException)
            {
                // 3 cases:
                // 1 - Request aborted, the client disconnected (no response)
                // 2 - The poll timeout is hit (204)
                // 3 - A new request comes in and cancels this request (205)

                // Case 1
                if (context.RequestAborted.IsCancellationRequested)
                {
                    // Don't count this as cancellation, this is normal as the poll can end due to the browser closing.
                    // The background thread will eventually dispose this connection if it's inactive
                    _logger.LogDebug("Client disconnected from Long Polling endpoint.");
                }
                // Case 2
                else if (_timeoutToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Poll request timed out. Sending 200 response.");

                    context.Response.ContentLength = 0;
                    context.Response.StatusCode = StatusCodes.Status200OK;
                }
                else
                {
                    // Case 3
                    _logger.LogInformation("Terminating Long Polling connection by sending 204 response.");
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Long Polling transport was terminated due to an error");
                throw;
            }
        }
    }
}
