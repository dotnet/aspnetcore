// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal partial class ServerSentEventsTransport : ITransport
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        // Volatile so that the SSE loop sees the updated value set from a different thread
        private volatile Exception? _error;
        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _inputCts = new CancellationTokenSource();
        private readonly ServerSentEventsMessageParser _parser = new ServerSentEventsMessageParser();
        private IDuplexPipe? _transport;
        private IDuplexPipe? _application;

        internal Task Running { get; private set; } = Task.CompletedTask;

        public PipeReader Input => _transport!.Input;

        public PipeWriter Output => _transport!.Output;

        public ServerSentEventsTransport(HttpClient httpClient)
            : this(httpClient, null)
        { }

        public ServerSentEventsTransport(HttpClient httpClient, ILoggerFactory? loggerFactory)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            _httpClient = httpClient;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ServerSentEventsTransport>();
        }

        public async Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken = default)
        {
            if (transferFormat != TransferFormat.Text)
            {
                throw new ArgumentException($"The '{transferFormat}' transfer format is not supported by this transport.", nameof(transferFormat));
            }

            Log.StartTransport(_logger, transferFormat);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            HttpResponseMessage? response = null;

            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                response?.Dispose();

                Log.TransportStopping(_logger);

                throw;
            }

            // Create the pipe pair (Application's writer is connected to Transport's reader, and vice versa)
            var options = ClientPipeOptions.DefaultOptions;
            var pair = DuplexPipe.CreateConnectionPair(options, options);

            _transport = pair.Transport;
            _application = pair.Application;

            // Cancellation token will be triggered when the pipe is stopped on the client.
            // This is to avoid the client throwing from a 404 response caused by the
            // server stopping the connection while the send message request is in progress.
            // _application.Input.OnWriterCompleted((exception, state) => ((CancellationTokenSource)state).Cancel(), inputCts);

            Running = ProcessAsync(url, response);
        }

        private async Task ProcessAsync(Uri url, HttpResponseMessage response)
        {
            Debug.Assert(_application != null);

            // Start sending and polling (ask for binary if the server supports it)
            var receiving = ProcessEventStream(response, _transportCts.Token);
            var sending = SendUtils.SendMessages(url, _application, _httpClient, _logger, _inputCts.Token);

            // Wait for send or receive to complete
            var trigger = await Task.WhenAny(receiving, sending);

            if (trigger == receiving)
            {
                // We're waiting for the application to finish and there are 2 things it could be doing
                // 1. Waiting for application data
                // 2. Waiting for an outgoing send (this should be instantaneous)

                _inputCts.Cancel();

                // Cancel the application so that ReadAsync yields
                _application.Input.CancelPendingRead();

                await sending;
            }
            else
            {
                // Set the sending error so we communicate that to the application
                _error = sending.IsFaulted ? sending.Exception!.InnerException : null;

                _transportCts.Cancel();

                // Cancel any pending flush so that we can quit
                _application.Output.CancelPendingFlush();

                await receiving;
            }
        }

        private async Task ProcessEventStream(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            Debug.Assert(_application != null);

            Log.StartReceive(_logger);

            static void CancelReader(object? state) => ((PipeReader)state!).CancelPendingRead();

            using (response)
#if NETCOREAPP
            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
#else
            using (var stream = await response.Content.ReadAsStreamAsync())
#endif
            {
                var reader = PipeReader.Create(stream);

                using var registration = cancellationToken.Register(CancelReader, reader);

                try
                {
                    while (true)
                    {
                        var result = await reader.ReadAsync(cancellationToken);
                        var buffer = result.Buffer;
                        var consumed = buffer.Start;
                        var examined = buffer.End;

                        try
                        {
                            if (result.IsCanceled)
                            {
                                Log.ReceiveCanceled(_logger);
                                break;
                            }

                            if (!buffer.IsEmpty)
                            {
                                Log.ParsingSSE(_logger, buffer.Length);

                                var parseResult = _parser.ParseMessage(buffer, out consumed, out examined, out var message);
                                FlushResult flushResult = default;

                                switch (parseResult)
                                {
                                    case ServerSentEventsMessageParser.ParseResult.Completed:
                                        Log.MessageToApplication(_logger, message!.Length);

                                        flushResult = await _application.Output.WriteAsync(message, cancellationToken);

                                        _parser.Reset();
                                        break;
                                    case ServerSentEventsMessageParser.ParseResult.Incomplete:
                                        if (result.IsCompleted)
                                        {
                                            throw new FormatException("Incomplete message.");
                                        }
                                        break;
                                }

                                // We canceled in the middle of applying back pressure
                                // or if the consumer is done
                                if (flushResult.IsCanceled || flushResult.IsCompleted)
                                {
                                    Log.EventStreamEnded(_logger);
                                    break;
                                }
                            }
                            else if (result.IsCompleted)
                            {
                                break;
                            }
                        }
                        finally
                        {
                            reader.AdvanceTo(consumed, examined);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _error = ex;
                }
                finally
                {
                    _application.Output.Complete(_error);

                    Log.ReceiveStopped(_logger);

                    reader.Complete();
                }
            }
        }

        public async Task StopAsync()
        {
            Log.TransportStopping(_logger);

            if (_application == null)
            {
                // We never started
                return;
            }

            _transport!.Output.Complete();
            _transport!.Input.Complete();

            _application.Input.CancelPendingRead();

            try
            {
                await Running;
            }
            catch (Exception ex)
            {
                Log.TransportStopped(_logger, ex);
                throw;
            }

            Log.TransportStopped(_logger, null);
        }
    }
}
