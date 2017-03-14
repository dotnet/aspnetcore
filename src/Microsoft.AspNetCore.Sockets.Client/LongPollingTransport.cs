// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

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
        private MessageParser _parser = new MessageParser();

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
            _logger.LogInformation("Starting {0}", nameof(LongPollingTransport));

            _application = application;

            // Start sending and polling
            _poller = Poll(Utils.AppendPath(url, "poll"), _transportCts.Token);
            _sender = SendMessages(Utils.AppendPath(url, "send"), _transportCts.Token);

            Running = Task.WhenAll(_sender, _poller).ContinueWith(t =>
            {
                _logger.LogDebug("Transport stopped. Exception: '{0}'", t.Exception?.InnerException);

                _application.Output.TryComplete(t.IsFaulted ? t.Exception.InnerException : null);
                return t;
            }).Unwrap();

            return TaskCache.CompletedTask;
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Transport {0} is stopping", nameof(LongPollingTransport));

            _transportCts.Cancel();

            try
            {
                await Running;
            }
            catch
            {
                // exceptions have been handled in the Running task continuation by closing the channel with the exception
            }

            _logger.LogInformation("Transport {0} stopped", nameof(LongPollingTransport));
        }

        private async Task Poll(Uri pollUrl, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting the receive loop");
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
                        _logger.LogDebug("The server is closing the connection");

                        // Transport closed or polling stopped, we're done
                        break;
                    }
                    else
                    {
                        _logger.LogDebug("Received messages from the server");

                        // Until Pipeline starts natively supporting BytesReader, this is the easiest way to do this.
                        var payload = await response.Content.ReadAsByteArrayAsync();
                        if (payload.Length > 0)
                        {
                            var messages = ParsePayload(payload);

                            foreach (var message in messages)
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

            _logger.LogInformation("Receive loop stopped");
        }

        private IList<Message> ParsePayload(byte[] payload)
        {
            var reader = new BytesReader(payload);
            var messageFormat = MessageParser.GetFormat(reader.Unread[0]);
            reader.Advance(1);

            _parser.Reset();
            var messages = new List<Message>();
            while (_parser.TryParseMessage(ref reader, messageFormat, out var message))
            {
                messages.Add(message);
            }

            // Since we pre-read the whole payload, we know that when this fails we have read everything.
            // Once Pipelines natively support BytesReader, we could get into situations where the data for
            // a message just isn't available yet.

            // If there's still data, we hit an incomplete message
            if (reader.Unread.Length > 0)
            {
                throw new FormatException("Incomplete message");
            }
            return messages;
        }

        private async Task SendMessages(Uri sendUrl, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting the send loop");

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

                        _logger.LogDebug("Sending a message to the server using url: '{0}'. Message type {1}", sendUrl, message.Type);

                        var response = await _httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode();

                        _logger.LogDebug("Message sent successfully");

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

            _logger.LogInformation("Send loop stopped");
        }
    }
}
