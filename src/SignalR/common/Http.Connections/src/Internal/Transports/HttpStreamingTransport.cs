// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal.Transports
{
    internal class HttpStreamingTransport : IHttpTransport
    {
        private readonly PipeReader _application;
        private readonly string _connectionId;
        private readonly ILogger _logger;

        public HttpStreamingTransport(PipeReader application, string connectionId, ILoggerFactory loggerFactory)
        {
            _application = application;
            _connectionId = connectionId;

            // We create the logger with a string to preserve the logging namespace after the server side transport renames.
            _logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Connections.Internal.Transports.HttpStreamingTransport");
        }

        public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
        {
            // Flush headers immediately so we can start streaming
            await context.Response.Body.FlushAsync();

            try
            {
                while (true)
                {
                    await _application.CopyToAsync(context.Response.Body, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Closed connection
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, long, Exception> _writingMessage =
                LoggerMessage.Define<long>(LogLevel.Trace, new EventId(1, "HttpStreamingWritingMessage"), "Writing a {Count} byte message.");

            public static void WritingMessage(ILogger logger, long count)
            {
                _writingMessage(logger, count, null);
            }
        }
    }
}
