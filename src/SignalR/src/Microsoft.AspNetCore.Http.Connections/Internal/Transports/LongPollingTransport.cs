// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports
{
    public class LongPollingTransport : IHttpTransport
    {
        private readonly PipeReader _application;
        private readonly ILogger _logger;
        private readonly CancellationToken _timeoutToken;

        public LongPollingTransport(CancellationToken timeoutToken, PipeReader application, ILoggerFactory loggerFactory)
        {
            _timeoutToken = timeoutToken;
            _application = application;
            _logger = loggerFactory.CreateLogger<LongPollingTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            try
            {
                var result = await _application.ReadAsync(token);
                var buffer = result.Buffer;

                if (buffer.IsEmpty && result.IsCompleted)
                {
                    Log.LongPolling204(_logger);
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                // We're intentionally not checking cancellation here because we need to drain messages we've got so far,
                // but it's too late to emit the 204 required by being canceled.

                Log.LongPollingWritingMessage(_logger, buffer.Length);

                context.Response.ContentLength = buffer.Length;
                context.Response.ContentType = "application/octet-stream";

                try
                {
                    await context.Response.Body.WriteAsync(buffer);
                }
                finally
                {
                    _application.AdvanceTo(buffer.End);
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
                    Log.LongPollingDisconnected(_logger);
                }
                // Case 2
                else if (_timeoutToken.IsCancellationRequested)
                {
                    Log.PollTimedOut(_logger);

                    context.Response.ContentLength = 0;
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                }
                else
                {
                    // Case 3
                    Log.LongPolling204(_logger);
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                }
            }
            catch (Exception ex)
            {
                Log.LongPollingTerminated(_logger, ex);
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                throw;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _longPolling204 =
                LoggerMessage.Define(LogLevel.Debug, new EventId(1, "LongPolling204"), "Terminating Long Polling connection by sending 204 response.");

            private static readonly Action<ILogger, Exception> _pollTimedOut =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "PollTimedOut"), "Poll request timed out. Sending 200 response to connection.");

            private static readonly Action<ILogger, long, Exception> _longPollingWritingMessage =
                LoggerMessage.Define<long>(LogLevel.Trace, new EventId(3, "LongPollingWritingMessage"), "Writing a {Count} byte message to connection.");

            private static readonly Action<ILogger, Exception> _longPollingDisconnected =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "LongPollingDisconnected"), "Client disconnected from Long Polling endpoint for connection.");

            private static readonly Action<ILogger, Exception> _longPollingTerminated =
                LoggerMessage.Define(LogLevel.Error, new EventId(5, "LongPollingTerminated"), "Long Polling transport was terminated due to an error on connection.");

            public static void LongPolling204(ILogger logger)
            {
                _longPolling204(logger, null);
            }

            public static void PollTimedOut(ILogger logger)
            {
                _pollTimedOut(logger, null);
            }

            public static void LongPollingWritingMessage(ILogger logger, long count)
            {
                _longPollingWritingMessage(logger, count, null);
            }

            public static void LongPollingDisconnected(ILogger logger)
            {
                _longPollingDisconnected(logger, null);
            }

            public static void LongPollingTerminated(ILogger logger, Exception ex)
            {
                _longPollingTerminated(logger, ex);
            }
        }
    }
}
