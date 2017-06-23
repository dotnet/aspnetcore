// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal static class SendUtils
    {
        private static readonly string DefaultUserAgent = "Microsoft.AspNetCore.SignalR.Client/0.0.0";
        public static readonly ProductInfoHeaderValue DefaultUserAgentHeader = ProductInfoHeaderValue.Parse(DefaultUserAgent);

        public static async Task SendMessages(Uri sendUrl, Channel<byte[], SendMessage> application, HttpClient httpClient, CancellationTokenSource transportCts, ILogger logger)
        {
            logger.LogInformation("Starting the send loop");
            IList<SendMessage> messages = null;
            try
            {
                while (await application.In.WaitToReadAsync(transportCts.Token))
                {
                    // Grab as many messages as we can from the channel
                    messages = new List<SendMessage>();
                    while (!transportCts.Token.IsCancellationRequested && application.In.TryRead(out SendMessage message))
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

                        foreach (var message in messages)
                        {
                            if (message.Payload != null)
                            {
                                memoryStream.Write(message.Payload, 0, message.Payload.Length);
                            }
                        }

                        memoryStream.Position = 0;

                        // Set the, now filled, stream as the content
                        request.Content = new StreamContent(memoryStream);

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
    }
}
