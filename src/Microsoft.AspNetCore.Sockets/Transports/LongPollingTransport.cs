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
        private readonly IReadableChannel<Message> _connection;
        private readonly ILogger _logger;

        public LongPollingTransport(IReadableChannel<Message> connection, ILoggerFactory loggerFactory)
        {
            _connection = connection;
            _logger = loggerFactory.CreateLogger<LongPollingTransport>();
        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
            if (_connection.Completion.IsCompleted)
            {
                // Client should stop if it receives a 204
                _logger.LogInformation("Terminating Long Polling connection by sending 204 response.");
                context.Response.StatusCode = 204;
                return;
            }

            try
            {
                // TODO: We need the ability to yield the connection without completing the channel.
                // This is to force ReadAsync to yield without data to end to poll but not the entire connection.
                // This is for cases when the client reconnects see issue #27
                using (var message = await _connection.ReadAsync(context.RequestAborted))
                {
                    _logger.LogDebug("Writing {0} byte message to response", message.Payload.Buffer.Length);
                    context.Response.ContentLength = message.Payload.Buffer.Length;
                    await message.Payload.Buffer.CopyToAsync(context.Response.Body);
                }
            }
            catch (Exception ex) when (ex.GetType().IsNested && ex.GetType().DeclaringType == typeof(Channel))
            {
                // The Channel was closed, while we were waiting to read. That's fine, just means we're done.
                // Gross that we have to catch this this way. See https://github.com/dotnet/corefxlab/issues/1068
            }
            catch (OperationCanceledException)
            {
                // Suppress the exception
                _logger.LogDebug("Client disconnected from Long Polling endpoint.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading next message from Application: {0}", ex);
                throw;
            }
        }
    }
}
