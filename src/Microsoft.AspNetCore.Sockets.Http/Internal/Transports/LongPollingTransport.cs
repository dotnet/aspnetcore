// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Internal.Transports
{
    public class LongPollingTransport : IHttpTransport
    {
        private readonly ChannelReader<byte[]> _application;
        private readonly ILogger _logger;
        private readonly CancellationToken _timeoutToken;
        private readonly string _connectionId;

        public LongPollingTransport(CancellationToken timeoutToken, ChannelReader<byte[]> application, string connectionId, ILoggerFactory loggerFactory)
        {
            _timeoutToken = timeoutToken;
            _application = application;
            _connectionId = connectionId;
            _logger = loggerFactory.CreateLogger<LongPollingTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            try
            {
                if (!await _application.WaitToReadAsync(token))
                {
                    await _application.Completion;
                    _logger.LongPolling204();
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                var contentLength = 0;
                var buffers = new List<byte[]>();
                // We're intentionally not checking cancellation here because we need to drain messages we've got so far,
                // but it's too late to emit the 204 required by being cancelled.
                while (_application.TryRead(out var buffer))
                {
                    contentLength += buffer.Length;
                    buffers.Add(buffer);

                    _logger.LongPollingWritingMessage(buffer.Length);
                }

                context.Response.ContentLength = contentLength;
                context.Response.ContentType = "application/octet-stream";

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
                // 3 - A new request comes in and cancels this request (204)

                // Case 1
                if (context.RequestAborted.IsCancellationRequested)
                {
                    // Don't count this as cancellation, this is normal as the poll can end due to the browser closing.
                    // The background thread will eventually dispose this connection if it's inactive
                    _logger.LongPollingDisconnected();
                }
                // Case 2
                else if (_timeoutToken.IsCancellationRequested)
                {
                    _logger.PollTimedOut();

                    context.Response.ContentLength = 0;
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                }
                else
                {
                    // Case 3
                    _logger.LongPolling204();
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LongPollingTerminated(ex);
                throw;
            }
        }
    }
}
