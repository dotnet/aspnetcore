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
    public class LongPollingTransport : IHttpTransport
    {
        // REVIEW: This size?
        internal const int MaxBufferSize = 4096;

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

                // TODO: Add support for binary protocol
                var messageFormat = MessageFormat.Text;
                context.Response.ContentType = MessageFormatter.GetContentType(messageFormat);

                var writer = context.Response.Body.AsPipelineWriter();
                var alloc = writer.Alloc(minimumSize: 1);
                alloc.WriteBigEndian(MessageFormatter.GetFormatIndicator(messageFormat));

                while (_application.TryRead(out var message))
                {
                    var buffer = alloc.Memory.Span;

                    _logger.LogDebug("Writing {0} byte message to response", message.Payload.Length);

                    // Try to format the message
                    if (!MessageFormatter.TryFormatMessage(message, buffer, messageFormat, out var written))
                    {
                        // We need to expand the buffer
                        // REVIEW: I'm not sure I fully understand the "right" pattern here...
                        alloc.Ensure(MaxBufferSize);
                        buffer = alloc.Memory.Span;

                        // Try one more time
                        if (!MessageFormatter.TryFormatMessage(message, buffer, messageFormat, out written))
                        {
                            // Message too large
                            throw new InvalidOperationException($"Message is too large to write. Maximum allowed message size is: {MaxBufferSize}");
                        }
                    }

                    // Update the buffer and commit
                    alloc.Advance(written);
                    alloc.Commit();
                    alloc = writer.Alloc();
                    buffer = alloc.Memory.Span;
                }

                await alloc.FlushAsync();
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
