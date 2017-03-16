// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

            // Start sending and polling (ask for binary if the server supports it)
            var pollUrl = Utils.AppendQueryString(Utils.AppendPath(url, "poll"), "supportsBinary=true");
            _poller = Poll(pollUrl, _transportCts.Token);
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

                        var messageFormat = MessageParser.GetFormatFromContentType(response.Content.Headers.ContentType.ToString());

                        // Until Pipeline starts natively supporting BytesReader, this is the easiest way to do this.
                        var payload = await response.Content.ReadAsByteArrayAsync();
                        if (payload.Length > 0)
                        {
                            var messages = ParsePayload(payload, messageFormat);

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
                _logger.LogInformation("Receive loop stopped");
            }
        }

        private IList<Message> ParsePayload(byte[] payload, MessageFormat messageFormat)
        {
            var reader = new BytesReader(payload);
            if (messageFormat != MessageParser.GetFormatFromIndicator(reader.Unread[0]))
            {
                throw new FormatException($"Format indicator '{(char)reader.Unread[0]}' does not match format determined by Content-Type '{MessageFormatter.GetContentType(messageFormat)}'");
            }
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
            IList<SendMessage> messages = null;
            try
            {
                while (await _application.Input.WaitToReadAsync(cancellationToken))
                {
                    // Grab as many messages as we can from the channel
                    messages = new List<SendMessage>();
                    while (!cancellationToken.IsCancellationRequested && _application.Input.TryRead(out SendMessage message))
                    {
                        messages.Add(message);
                    }

                    if (messages.Count > 0)
                    {
                        _logger.LogDebug("Sending {0} message(s) to the server using url: {1}", messages.Count, sendUrl);

                        // Send them in a single post
                        var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
                        request.Headers.UserAgent.Add(DefaultUserAgentHeader);

                        // TODO: We can probably use a pipeline here or some kind of pooled memory.
                        // But where do we get the pool from? ArrayBufferPool.Instance?
                        var memoryStream = new MemoryStream();

                        // Write the messages to the stream
                        var pipe = memoryStream.AsPipelineWriter();
                        var output = new PipelineTextOutput(pipe, TextEncoder.Utf8); // We don't need the Encoder, but it's harmless to set.
                        await WriteMessagesAsync(messages, output, MessageFormat.Binary);

                        // Seek back to the start
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        // Set the, now filled, stream as the content
                        request.Content = new StreamContent(memoryStream);
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(MessageFormatter.GetContentType(MessageFormat.Binary));

                        var response = await _httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode();

                        _logger.LogDebug("Message(s) sent successfully");
                        foreach (var message in messages)
                        {
                            message.SendResult?.TrySetResult(null);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No messages in batch to send");
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
                _logger.LogError("Error while sending to '{0}': {1}", sendUrl, ex);
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
                _transportCts.Cancel();
            }

            _logger.LogInformation("Send loop stopped");
        }

        private async Task WriteMessagesAsync(IList<SendMessage> messages, PipelineTextOutput output, MessageFormat format)
        {
            output.Append(MessageFormatter.GetFormatIndicator(format), TextEncoder.Utf8);

            foreach (var message in messages)
            {
                _logger.LogDebug("Writing '{0}' message to the server", message.Type);

                var payload = message.Payload ?? Array.Empty<byte>();
                if (!MessageFormatter.TryWriteMessage(new Message(payload, message.Type, endOfMessage: true), output, format))
                {
                    // We didn't get any more memory!
                    throw new InvalidOperationException("Unable to write message to pipeline");
                }
                await output.FlushAsync();
            }
        }
    }
}
