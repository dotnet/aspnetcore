// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal static class SendUtils
    {
        public static async Task SendMessages(Uri sendUrl, Channel<byte[], SendMessage> application, HttpClient httpClient,
            HttpOptions httpOptions, CancellationTokenSource transportCts, ILogger logger)
        {
            logger.SendStarted();
            IList<SendMessage> messages = null;
            try
            {
                while (await application.Reader.WaitToReadAsync(transportCts.Token))
                {
                    // Grab as many messages as we can from the channel
                    messages = new List<SendMessage>();
                    while (!transportCts.IsCancellationRequested && application.Reader.TryRead(out SendMessage message))
                    {
                        messages.Add(message);
                    }

                    transportCts.Token.ThrowIfCancellationRequested();
                    if (messages.Count > 0)
                    {
                        logger.SendingMessages(messages.Count, sendUrl);

                        // Send them in a single post
                        var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                        PrepareHttpRequest(request, httpOptions);

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

                        var response = await httpClient.SendAsync(request, transportCts.Token);
                        response.EnsureSuccessStatusCode();

                        logger.SentSuccessfully();
                        foreach (var message in messages)
                        {
                            message.SendResult?.TrySetResult(null);
                        }
                    }
                    else
                    {
                        logger.NoMessages();
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
                logger.SendCanceled();
            }
            catch (Exception ex)
            {
                logger.ErrorSending(sendUrl, ex);
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

            logger.SendStopped();
        }

        public static void PrepareHttpRequest(HttpRequestMessage request, HttpOptions httpOptions)
        {
            if (httpOptions?.Headers != null)
            {
                foreach (var header in httpOptions.Headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            request.Headers.UserAgent.Add(Constants.UserAgentHeader);

            if (httpOptions?.JwtBearerTokenFactory != null)
            {
                request.Headers.Add("Authorization", $"Bearer {httpOptions.JwtBearerTokenFactory()}");
            }
        }
    }
}
