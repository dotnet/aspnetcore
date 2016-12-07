// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class LongPollingTransport : ITransport
    {
        private static readonly string DefaultUserAgent = "Microsoft.AspNetCore.SignalR.Client/0.0.0";
        private static readonly ProductInfoHeaderValue DefaultUserAgentHeader = ProductInfoHeaderValue.Parse(DefaultUserAgent);

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _senderCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _pollCts = new CancellationTokenSource();

        private IPipelineConnection _pipeline;
        private Task _sender;
        private Task _poller;

        public Task Running { get; private set; }

        public LongPollingTransport(HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _logger = loggerFactory.CreateLogger<LongPollingTransport>();
        }

        public void Dispose()
        {
            _senderCts.Cancel();
            _pollCts.Cancel();
            _pipeline?.Dispose();
        }

        public Task StartAsync(Uri url, IPipelineConnection pipeline)
        {
            _pipeline = pipeline;

            // Schedule shutdown of the poller when the output is closed
            pipeline.Output.Writing.ContinueWith(_ =>
            {
                _pollCts.Cancel();
                return TaskCache.CompletedTask;
            });

            // Start sending and polling
            _poller = Poll(Utils.AppendPath(url, "poll"), _pollCts.Token);
            _sender = SendMessages(Utils.AppendPath(url, "send"), _senderCts.Token);
            Running = Task.WhenAll(_sender, _poller);

            return TaskCache.CompletedTask;
        }

        private async Task Poll(Uri pollUrl, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, pollUrl);
                    request.Headers.UserAgent.Add(DefaultUserAgentHeader);

                    var response = await _httpClient.SendAsync(request, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    if (response.StatusCode == HttpStatusCode.NoContent || cancellationToken.IsCancellationRequested)
                    {
                        // Transport closed or polling stopped, we're done
                        break;
                    }
                    else
                    {
                        // Write the data to the output
                        var buffer = _pipeline.Output.Alloc();
                        var stream = new WriteableBufferStream(buffer);
                        await response.Content.CopyToAsync(stream);
                        await buffer.FlushAsync();
                    }
                }

                // Polling complete
                _pipeline.Output.Complete();
            }
            catch (Exception ex)
            {
                // Shut down the output pipeline and log
                _logger.LogError("Error while polling '{0}': {1}", pollUrl, ex);
                _pipeline.Output.Complete(ex);
                _pipeline.Input.Complete(ex);
            }
        }

        private async Task SendMessages(Uri sendUrl, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() => _pipeline.Input.Complete()))
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await _pipeline.Input.ReadAsync();
                        var buffer = result.Buffer;
                        if (buffer.IsEmpty || result.IsCompleted)
                        {
                            // No more data to send
                            break;
                        }

                        // Create a message to send
                        var message = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                        message.Headers.UserAgent.Add(DefaultUserAgentHeader);
                        message.Content = new ReadableBufferContent(buffer);

                        // Send it
                        var response = await _httpClient.SendAsync(message);
                        response.EnsureSuccessStatusCode();

                        _pipeline.Input.Advance(buffer.End);
                    }

                    // Sending complete
                    _pipeline.Input.Complete();
                }
                catch (Exception ex)
                {
                    // Shut down the input pipeline and log
                    _logger.LogError("Error while sending to '{0}': {1}", sendUrl, ex);
                    _pipeline.Input.Complete(ex);
                }
            }
        }
    }
}
