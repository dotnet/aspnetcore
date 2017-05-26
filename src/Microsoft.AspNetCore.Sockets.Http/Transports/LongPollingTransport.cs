// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Text;
using System.Text.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Transports
{
    public class LongPollingTransport : IHttpTransport
    {
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
                    await _application.Completion;
                    _logger.LogInformation("Terminating Long Polling connection by sending 204 response.");
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                var headers = context.Request.GetTypedHeaders();
                var messageFormat = headers.Accept?.Contains(new Net.Http.Headers.MediaTypeHeaderValue(MessageFormatter.BinaryContentType)) == true ?
                MessageFormat.Binary :
                MessageFormat.Text;
                context.Response.ContentType = MessageFormatter.GetContentType(messageFormat);

                var writer = context.Response.Body.AsPipelineWriter();
                var output = new PipelineTextOutput(writer, TextEncoder.Utf8); // We don't need the Encoder, but it's harmless to set.

                output.Append(MessageFormatter.GetFormatIndicator(messageFormat));

                // We're intentionally not checking cancellation here because we need to drain messages we've got so far,
                // but it's too late to emit the 204 required by being cancelled.
                while (_application.TryRead(out var message))
                {
                    _logger.LogDebug("Writing {0} byte message to response", message.Payload.Length);

                    if (!MessageFormatter.TryWriteMessage(message, output, messageFormat))
                    {
                        // We ran out of space to write, even after trying to enlarge.
                        // This should only happen in a significant lack-of-memory scenario.

                        // IOutput doesn't really have a way to write incremental

                        // Throwing InvalidOperationException here, but it's not quite an invalid operation...
                        throw new InvalidOperationException("Ran out of space to format messages!");
                    }

                    // REVIEW: Flushing after each message? Good? Bad? We can't access Commit because it's hidden inside PipelineTextOutput
                    await output.FlushAsync();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Long Polling transport was terminated due to an error");
                throw;
            }
        }
    }
}
