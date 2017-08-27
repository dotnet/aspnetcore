// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal static class SendUtils
    {
        public static async Task SendMessages(Uri sendUrl, Channel<byte[], SendMessage> application, HttpClient httpClient,
            CancellationTokenSource transportCts, ILogger logger, string connectionId)
        {
            logger.SendStarted(connectionId);
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
                        logger.SendingMessages(connectionId, messages.Count, sendUrl);

                        // Send them in a single post
                        var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                        request.Headers.UserAgent.Add(Constants.UserAgentHeader);

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

                        logger.SentSuccessfully(connectionId);
                        foreach (var message in messages)
                        {
                            message.SendResult?.TrySetResult(null);
                        }
                    }
                    else
                    {
                        logger.NoMessages(connectionId);
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
                logger.SendCanceled(connectionId);
            }
            catch (Exception ex)
            {
                logger.ErrorSending(connectionId, sendUrl, ex);
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

            logger.SendStopped(connectionId);
        }
    }
}
