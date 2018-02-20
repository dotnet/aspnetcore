// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal static class SendUtils
    {
        public static async Task SendMessages(Uri sendUrl, IDuplexPipe application, HttpClient httpClient,
            HttpOptions httpOptions, CancellationTokenSource transportCts, ILogger logger)
        {
            logger.SendStarted();

            try
            {
                while (true)
                {
                    var result = await application.Input.ReadAsync(transportCts.Token);
                    var buffer = result.Buffer;

                    try
                    {
                        // Grab as many messages as we can from the channel

                        transportCts.Token.ThrowIfCancellationRequested();
                        if (!buffer.IsEmpty)
                        {
                            logger.SendingMessages(buffer.Length, sendUrl);

                            // Send them in a single post
                            var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                            PrepareHttpRequest(request, httpOptions);

                            request.Content = new ReadOnlyBufferContent(buffer);

                            var response = await httpClient.SendAsync(request, transportCts.Token);
                            response.EnsureSuccessStatusCode();

                            logger.SentSuccessfully();
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                        else
                        {
                            logger.NoMessages();
                        }
                    }
                    finally
                    {
                        application.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.SendCanceled();
            }
            catch (Exception ex)
            {
                logger.ErrorSending(sendUrl, ex);
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

            if (httpOptions?.AccessTokenFactory != null)
            {
                request.Headers.Add("Authorization", $"Bearer {httpOptions.AccessTokenFactory()}");
            }
        }

        private class ReadOnlyBufferContent : HttpContent
        {
            private readonly ReadOnlyBuffer<byte> _buffer;

            public ReadOnlyBufferContent(ReadOnlyBuffer<byte> buffer)
            {
                _buffer = buffer;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return stream.WriteAsync(_buffer);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _buffer.Length;
                return true;
            }
        }
    }
}
