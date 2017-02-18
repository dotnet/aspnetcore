// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.AspNetCore.Sockets.Formatters;
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
        private IChannelConnection<SendMessage, Message> _application;
        private Task _sender;
        private Task _poller;
        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();

        public Task Running { get; private set; } = Task.CompletedTask;

        public LongPollingTransport(HttpClient httpClient) 
            : this(httpClient, null)
        { }

        public LongPollingTransport(HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LongPollingTransport>();
        }

        public Task StartAsync(Uri url, IChannelConnection<SendMessage, Message> application)
        {
            _application = application;

            // Start sending and polling
            _poller = Poll(Utils.AppendPath(url, "poll"), _transportCts.Token);
            _sender = SendMessages(Utils.AppendPath(url, "send"), _transportCts.Token);

            Running = Task.WhenAll(_sender, _poller).ContinueWith(t =>
            {
                _application.Output.TryComplete(t.IsFaulted ? t.Exception.InnerException : null);
                return t;
            }).Unwrap();

            return TaskCache.CompletedTask;
        }

        public async Task StopAsync()
        {
            _transportCts.Cancel();
            try
            {
                await Running;
            }
            catch
            {
                // exceptions have been handled in the Running task continuation by closing the channel with the exception
            }
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
                        // Read the whole payload
                        var payload = await response.Content.ReadAsByteArrayAsync();

                        foreach (var message in ReadMessages(payload))
                        {
                            while (!_application.Output.TryWrite(message))
                            {
                                if (cancellationToken.IsCancellationRequested || !await _application.Output.WaitToWriteAsync(cancellationToken))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // transport is being closed
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while polling '{0}': {1}", pollUrl, ex);
                throw;
            }
            finally
            {
                // Make sure the send loop is terminated
                _transportCts.Cancel();
            }
        }

        private IEnumerable<Message> ReadMessages(ReadOnlySpan<byte> payload)
        {
            if (payload.Length == 0)
            {
                yield break;
            }

            var messageFormat = MessageFormatter.GetFormat(payload[0]);
            payload = payload.Slice(1);

            while (payload.Length > 0)
            {
                if (!MessageFormatter.TryParseMessage(payload, messageFormat, out var message, out var consumed))
                {
                    throw new InvalidDataException("Invalid message payload from server");
                }

                payload = payload.Slice(consumed);
                yield return message;
            }
        }

        private async Task SendMessages(Uri sendUrl, CancellationToken cancellationToken)
        {
            TaskCompletionSource<object> sendTcs = null;
            try
            {
                while (await _application.Input.WaitToReadAsync(cancellationToken))
                {
                    while (!cancellationToken.IsCancellationRequested && _application.Input.TryRead(out SendMessage message))
                    {
                        sendTcs = message.SendResult;
                        var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                        request.Headers.UserAgent.Add(DefaultUserAgentHeader);

                        if (message.Payload != null && message.Payload.Length > 0)
                        {
                            request.Content = new ByteArrayContent(message.Payload);
                        }

                        var response = await _httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        sendTcs.SetResult(null);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // transport is being closed
                sendTcs?.TrySetCanceled();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while sending to '{0}': {1}", sendUrl, ex);
                sendTcs?.TrySetException(ex);
                throw;
            }
            finally
            {
                // Make sure the poll loop is terminated
                _transportCts.Cancel();
            }
        }
    }
}
