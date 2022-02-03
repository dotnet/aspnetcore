// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports;

internal class ServerSentEventsServerTransport : IHttpTransport
{
    private readonly PipeReader _application;
    private readonly string _connectionId;
    private readonly ILogger _logger;
    private readonly HttpConnectionContext? _connection;

    public ServerSentEventsServerTransport(PipeReader application, string connectionId, ILoggerFactory loggerFactory)
        : this(application, connectionId, connection: null, loggerFactory)
    { }

    public ServerSentEventsServerTransport(PipeReader application, string connectionId, HttpConnectionContext? connection, ILoggerFactory loggerFactory)
    {
        _application = application;
        _connectionId = connectionId;
        _connection = connection;

        // We create the logger with a string to preserve the logging namespace after the server side transport renames.
        _logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Connections.Internal.Transports.ServerSentEventsTransport");
    }

    public async Task ProcessRequestAsync(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache,no-store";
        context.Response.Headers.Pragma = "no-cache";

        // Make sure we disable all response buffering for SSE
        var bufferingFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
        bufferingFeature.DisableBuffering();

        context.Response.Headers.ContentEncoding = "identity";

        try
        {
            // Workaround for a Firefox bug where EventSource won't fire the open event
            // until it receives some data
            await context.Response.WriteAsync(":\r\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
            while (true)
            {
                var result = await _application.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                try
                {
                    if (result.IsCanceled)
                    {
                        break;
                    }

                    if (!buffer.IsEmpty)
                    {
                        Log.SSEWritingMessage(_logger, buffer.Length);

                        _connection?.StartSendCancellation();
                        await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, context.Response.Body, _connection?.SendingToken ?? default);
                    }
                    else if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    _connection?.StopSendCancellation();
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
        private static readonly Action<ILogger, long, Exception?> _sseWritingMessage =
            LoggerMessage.Define<long>(LogLevel.Trace, new EventId(1, "SSEWritingMessage"), "Writing a {Count} byte message.");

        public static void SSEWritingMessage(ILogger logger, long count)
        {
            _sseWritingMessage(logger, count, null);
        }
    }
}
