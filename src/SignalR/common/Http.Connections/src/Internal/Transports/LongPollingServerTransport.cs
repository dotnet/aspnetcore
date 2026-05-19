// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports;

internal sealed partial class LongPollingServerTransport : IHttpTransport
{
    private readonly PipeReader _application;
    private readonly ILogger _logger;
    private readonly CancellationToken _timeoutToken;
    private readonly HttpConnectionContext? _connection;

    public LongPollingServerTransport(CancellationToken timeoutToken, PipeReader application, ILoggerFactory loggerFactory)
        : this(timeoutToken, application, loggerFactory, connection: null)
    { }

    public LongPollingServerTransport(CancellationToken timeoutToken, PipeReader application, ILoggerFactory loggerFactory, HttpConnectionContext? connection)
    {
        _timeoutToken = timeoutToken;
        _application = application;

        _connection = connection;

        // We create the logger with a string to preserve the logging namespace after the server side transport renames.
        _logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Connections.Internal.Transports.LongPollingTransport");
    }

    public async Task<bool> ProcessRequestAsync(HttpContext context, CancellationToken token)
    {
        try
        {
            var result = await _application.ReadAsync(token);
            var buffer = result.Buffer;

            try
            {
                if (buffer.IsEmpty && (result.IsCompleted || result.IsCanceled))
                {
                    Log.LongPolling204(_logger);
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return false;
                }

                // We're intentionally not checking cancellation here because we need to drain messages we've got so far,
                // but it's too late to emit the 204 required by being canceled.

                Log.LongPollingWritingMessage(_logger, buffer.Length);

                context.Response.ContentLength = buffer.Length;
                context.Response.ContentType = "application/octet-stream";

                _connection?.StartSendCancellation();
                await context.Response.Body.WriteAsync(buffer, _connection?.SendingToken ?? default);
            }
            finally
            {
                _connection?.StopSendCancellation();
                _application.AdvanceTo(buffer.End);
            }
        }
        catch (OperationCanceledException)
        {
            // 4 cases:
            // 1 - Request aborted, the client disconnected (no response)
            // 2 - The poll timeout is hit (200)
            // 3 - SendingToken was canceled, abort the connection
            // 4 - A new request comes in and cancels this request (204)

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
            else if (_connection?.SendingToken.IsCancellationRequested == true)
            {
                // Case 3
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                throw;
            }
            else
            {
                // Case 4
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
        return false;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Terminating Long Polling connection by sending 204 response.", EventName = "LongPolling204")]
        public static partial void LongPolling204(ILogger logger);

        [LoggerMessage(2, LogLevel.Debug, "Poll request timed out. Sending 200 response to connection.", EventName = "PollTimedOut")]
        public static partial void PollTimedOut(ILogger logger);

        [LoggerMessage(3, LogLevel.Trace, "Writing a {Count} byte message to connection.", EventName = "LongPollingWritingMessage")]
        public static partial void LongPollingWritingMessage(ILogger logger, long count);

        [LoggerMessage(4, LogLevel.Debug, "Client disconnected from Long Polling endpoint for connection.", EventName = "LongPollingDisconnected")]
        public static partial void LongPollingDisconnected(ILogger logger);

        [LoggerMessage(5, LogLevel.Error, "Long Polling transport was terminated due to an error on connection.", EventName = "LongPollingTerminated")]
        public static partial void LongPollingTerminated(ILogger logger, Exception ex);
    }
}
