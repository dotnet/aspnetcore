// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal static class SendUtils
    {
        private static readonly string DefaultUserAgent = "Microsoft.AspNetCore.SignalR.Client/0.0.0";
        public static readonly ProductInfoHeaderValue DefaultUserAgentHeader = ProductInfoHeaderValue.Parse(DefaultUserAgent);

        public static async Task SendMessages(Uri sendUrl, IChannelConnection<SendMessage, Message> application, HttpClient httpClient, CancellationTokenSource transportCts, ILogger logger)
        {
            logger.LogInformation("Starting the send loop");
            IList<SendMessage> messages = null;
            try
            {
                while (await application.Input.WaitToReadAsync(transportCts.Token))
                {
                    // Grab as many messages as we can from the channel
                    messages = new List<SendMessage>();
                    while (!transportCts.Token.IsCancellationRequested && application.Input.TryRead(out SendMessage message))
                    {
                        messages.Add(message);
                    }

                    if (messages.Count > 0)
                    {
                        logger.LogDebug("Sending {0} message(s) to the server using url: {1}", messages.Count, sendUrl);

                        // Send them in a single post
                        var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                        request.Headers.UserAgent.Add(DefaultUserAgentHeader);

                        // TODO: We can probably use a pipeline here or some kind of pooled memory.
                        // But where do we get the pool from? ArrayBufferPool.Instance?
                        var memoryStream = new MemoryStream();

                        // Write the messages to the stream
                        var pipe = memoryStream.AsPipelineWriter();
                        var output = new PipelineTextOutput(pipe, TextEncoder.Utf8); // We don't need the Encoder, but it's harmless to set.
                        await WriteMessagesAsync(messages, output, MessageFormat.Binary, logger);

                        // Seek back to the start
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        // Set the, now filled, stream as the content
                        request.Content = new StreamContent(memoryStream);
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(MessageFormatter.GetContentType(MessageFormat.Binary));

                        var response = await httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode();

                        logger.LogDebug("Message(s) sent successfully");
                        foreach (var message in messages)
                        {
                            message.SendResult?.TrySetResult(null);
                        }
                    }
                    else
                    {
                        logger.LogDebug("No messages in batch to send");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // transport is being closed
                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        // This will no-op for any messages that were already marked as completed.
                        message.SendResult?.TrySetCanceled();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error while sending to '{0}': {1}", sendUrl, ex);
                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        // This will no-op for any messages that were already marked as completed.
                        message.SendResult?.TrySetException(ex);
                    }
                }
                throw;
            }
            finally
            {
                // Make sure the poll loop is terminated
                transportCts.Cancel();
            }

            logger.LogInformation("Send loop stopped");
        }

        private static async Task WriteMessagesAsync(IList<SendMessage> messages, PipelineTextOutput output, MessageFormat format, ILogger logger)
        {
            output.Append(MessageFormatter.GetFormatIndicator(format), TextEncoder.Utf8);

            foreach (var message in messages)
            {
                logger.LogDebug("Writing '{0}' message to the server", message.Type);

                var payload = message.Payload ?? Array.Empty<byte>();
                if (!MessageFormatter.TryWriteMessage(new Message(payload, message.Type), output, format))
                {
                    // We didn't get any more memory!
                    throw new InvalidOperationException("Unable to write message to pipeline");
                }
                await output.FlushAsync();
            }
        }
    }
}
