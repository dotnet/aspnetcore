// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.AspNetCore.Sockets.Http.Internal;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class HttpConnection : IConnection
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private volatile int _connectionState = ConnectionState.Initial;
        private volatile ChannelConnection<byte[], SendMessage> _transportChannel;
        private readonly HttpClient _httpClient;
        private volatile ITransport _transport;
        private volatile Task _receiveLoopTask;
        private TaskCompletionSource<object> _startTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskQueue _eventQueue = new TaskQueue();
        private readonly ITransportFactory _transportFactory;
        private string _connectionId;
        private readonly TimeSpan _eventQueueDrainTimeout = TimeSpan.FromSeconds(5);

        private ReadableChannel<byte[]> Input => _transportChannel.In;
        private WritableChannel<SendMessage> Output => _transportChannel.Out;

        public Uri Url { get; }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public event Func<Task> Connected;
        public event Func<byte[], Task> Received;
        public event Func<Exception, Task> Closed;

        public HttpConnection(Uri url)
            : this(url, TransportType.All)
        { }

        public HttpConnection(Uri url, HttpMessageHandler httpMessageHandler)
            : this(url, TransportType.All, loggerFactory: null, httpMessageHandler: httpMessageHandler)
        { }

        public HttpConnection(Uri url, TransportType transportType)
            : this(url, transportType, loggerFactory: null)
        {
        }

        public HttpConnection(Uri url, ILoggerFactory loggerFactory)
            : this(url, TransportType.All, loggerFactory, httpMessageHandler: null)
        {
        }

        public HttpConnection(Uri url, TransportType transportType, ILoggerFactory loggerFactory)
            : this(url, transportType, loggerFactory, httpMessageHandler: null)
        {
        }

        public HttpConnection(Uri url, TransportType transportType, ILoggerFactory loggerFactory, HttpMessageHandler httpMessageHandler)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));

            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<HttpConnection>();
            _httpClient = httpMessageHandler == null ? new HttpClient() : new HttpClient(httpMessageHandler);
            _transportFactory = new DefaultTransportFactory(transportType, _loggerFactory, _httpClient);
        }

        public HttpConnection(Uri url, ITransportFactory transportFactory, ILoggerFactory loggerFactory, HttpMessageHandler httpMessageHandler)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<HttpConnection>();
            _httpClient = httpMessageHandler == null ? new HttpClient() : new HttpClient(httpMessageHandler);
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
        }

        public async Task StartAsync() => await StartAsyncCore().ForceAsync();

        private Task StartAsyncCore()
        {
            if (Interlocked.CompareExchange(ref _connectionState, ConnectionState.Connecting, ConnectionState.Initial)
                != ConnectionState.Initial)
            {
                return Task.FromException(
                    new InvalidOperationException("Cannot start a connection that is not in the Initial state."));
            }

            StartAsyncInternal()
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _startTcs.SetException(t.Exception.InnerException);
                    }
                    else if (t.IsCanceled)
                    {
                        _startTcs.SetCanceled();
                    }
                    else
                    {
                        _startTcs.SetResult(null);
                    }
                });

            return _startTcs.Task;
        }

        private async Task StartAsyncInternal()
        {
            _logger.HttpConnectionStarting();

            try
            {
                var negotiationResponse = await Negotiate(Url, _httpClient, _logger);
                _connectionId = negotiationResponse.ConnectionId;

                // Connection is being stopped while start was in progress
                if (_connectionState == ConnectionState.Disconnected)
                {
                    _logger.HttpConnectionClosed(_connectionId);
                    return;
                }

                _transport = _transportFactory.CreateTransport(GetAvailableServerTransports(negotiationResponse));

                var connectUrl = CreateConnectUrl(Url, negotiationResponse);
                _logger.StartingTransport(_connectionId, _transport.GetType().Name, connectUrl);
                await StartTransport(connectUrl);
            }
            catch
            {
                Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected);
                throw;
            }

            // if the connection is not in the Connecting state here it means the user called DisposeAsync
            if (Interlocked.CompareExchange(ref _connectionState, ConnectionState.Connected, ConnectionState.Connecting)
                == ConnectionState.Connecting)
            {
                var ignore = _eventQueue.Enqueue(() =>
                {
                    _logger.RaiseConnected(_connectionId);

                    Connected?.Invoke();

                    return Task.CompletedTask;
                });

                ignore = Input.Completion.ContinueWith(async t =>
                {
                    Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected);

                    // There is an inherent race between receive and close. Removing the last message from the channel
                    // makes Input.Completion task completed and runs this continuation. We need to await _receiveLoopTask
                    // to make sure that the message removed from the channel is processed before we drain the queue.
                    // There is a short window between we start the channel and assign the _receiveLoopTask a value.
                    // To make sure that _receiveLoopTask can be awaited (i.e. is not null) we need to await _startTask.
                    _logger.ProcessRemainingMessages(_connectionId);

                    await _startTcs.Task;
                    await _receiveLoopTask;

                    _logger.DrainEvents(_connectionId);
                    await _eventQueue.Drain();

                    await Task.WhenAny(_eventQueue.Drain().NoThrow(), Task.Delay(_eventQueueDrainTimeout));
                    _httpClient.Dispose();

                    _logger.RaiseClosed(_connectionId);

                    Closed?.Invoke(t.IsFaulted ? t.Exception.InnerException : null);

                    return Task.CompletedTask;
                });

                // start receive loop only after the Connected event was raised to
                // avoid Received event being raised before the Connected event
                _receiveLoopTask = ReceiveAsync();
            }
        }

        private async static Task<NegotiationResponse> Negotiate(Uri url, HttpClient httpClient, ILogger logger)
        {
            try
            {
                // Get a connection ID from the server
                logger.EstablishingConnection(url);
                using (var request = new HttpRequestMessage(HttpMethod.Options, url))
                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return await ParseNegotiateResponse(response, logger);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorWithNegotiation(url, ex);
                throw;
            }
        }

        private static async Task<NegotiationResponse> ParseNegotiateResponse(HttpResponseMessage response, ILogger logger)
        {
            NegotiationResponse negotiationResponse;
            using (var reader = new JsonTextReader(new StreamReader(await response.Content.ReadAsStreamAsync())))
            {
                try
                {
                    negotiationResponse = new JsonSerializer().Deserialize<NegotiationResponse>(reader);
                }
                catch (Exception ex)
                {
                    throw new FormatException("Invalid negotiation response received.", ex);
                }
            }

            if (negotiationResponse == null)
            {
                throw new FormatException("Invalid negotiation response received.");
            }

            return negotiationResponse;
        }

        private TransportType GetAvailableServerTransports(NegotiationResponse negotiationResponse)
        {
            if (negotiationResponse.AvailableTransports == null)
            {
                throw new FormatException("No transports returned in negotiation response.");
            }

            var availableServerTransports = (TransportType)0;
            foreach (var t in negotiationResponse.AvailableTransports)
            {
                availableServerTransports |= t;
            }

            return availableServerTransports;
        }

        private static Uri CreateConnectUrl(Uri url, NegotiationResponse negotiationResponse)
        {
            if (string.IsNullOrWhiteSpace(negotiationResponse.ConnectionId))
            {
                throw new FormatException("Invalid connection id returned in negotiation response.");
            }

            return Utils.AppendQueryString(url, "id=" + negotiationResponse.ConnectionId);
        }

        private async Task StartTransport(Uri connectUrl)
        {
            var applicationToTransport = Channel.CreateUnbounded<SendMessage>();
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationSide = ChannelConnection.Create(applicationToTransport, transportToApplication);
            _transportChannel = ChannelConnection.Create(transportToApplication, applicationToTransport);

            // Start the transport, giving it one end of the pipeline
            try
            {
                await _transport.StartAsync(connectUrl, applicationSide, requestedTransferMode: GetTransferMode(), connectionId: _connectionId);

                // actual transfer mode can differ from the one that was requested so set it on the feature
                Debug.Assert(_transport.Mode.HasValue, "transfer mode not set after transport started");
                SetTransferMode(_transport.Mode.Value);
            }
            catch (Exception ex)
            {
                _logger.ErrorStartingTransport(_connectionId, _transport.GetType().Name, ex);
                throw;
            }
        }

        private TransferMode GetTransferMode()
        {
            var transferModeFeature = Features.Get<ITransferModeFeature>();
            if (transferModeFeature == null)
            {
                return TransferMode.Text;
            }

            return transferModeFeature.TransferMode;
        }

        private void SetTransferMode(TransferMode transferMode)
        {
            var transferModeFeature = Features.Get<ITransferModeFeature>();
            if (transferModeFeature == null)
            {
                transferModeFeature = new TransferModeFeature();
                Features.Set(transferModeFeature);
            }

            transferModeFeature.TransferMode = transferMode;
        }

        private async Task ReceiveAsync()
        {
            try
            {
                _logger.HttpReceiveStarted(_connectionId);

                while (await Input.WaitToReadAsync())
                {
                    if (_connectionState != ConnectionState.Connected)
                    {
                        _logger.SkipRaisingReceiveEvent(_connectionId);
                        // drain
                        Input.TryRead(out _);
                        continue;
                    }

                    if (Input.TryRead(out var buffer))
                    {
                        _logger.ScheduleReceiveEvent(_connectionId);
                        _ = _eventQueue.Enqueue(() =>
                        {
                            _logger.RaiseReceiveEvent(_connectionId);

                            // Making a copy of the Received handler to ensure that its not null
                            // Can't use the ? operator because we specifically want to check if the handler is null
                            var receivedHandler = Received;
                            if (receivedHandler != null)
                            {
                                return receivedHandler(buffer);
                            }

                            return Task.CompletedTask;
                        });
                    }
                    else
                    {
                        _logger.FailedReadingMessage(_connectionId);
                    }
                }

                await Input.Completion;
            }
            catch (Exception ex)
            {
                Output.TryComplete(ex);
                _logger.ErrorReceiving(_connectionId, ex);
            }

            _logger.EndReceive(_connectionId);
        }

        public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default(CancellationToken)) =>
            await SendAsyncCore(data, cancellationToken).ForceAsync();

        private async Task SendAsyncCore(byte[] data, CancellationToken cancellationToken)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_connectionState != ConnectionState.Connected)
            {
                throw new InvalidOperationException(
                    "Cannot send messages when the connection is not in the Connected state.");
            }

            // TaskCreationOptions.RunContinuationsAsynchronously ensures that continuations awaiting
            // SendAsync (i.e. user's code) are not running on the same thread as the code that sets
            // TaskCompletionSource result. This way we prevent from user's code blocking our channel
            // send loop.
            var sendTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var message = new SendMessage(data, sendTcs);

            _logger.SendingMessage(_connectionId);

            while (await Output.WaitToWriteAsync(cancellationToken))
            {
                if (Output.TryWrite(message))
                {
                    await sendTcs.Task;
                    break;
                }
            }
        }

        public async Task DisposeAsync() => await DisposeAsyncCore().ForceAsync();

        private async Task DisposeAsyncCore()
        {
            _logger.StoppingClient(_connectionId);

            if (Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected) == ConnectionState.Initial)
            {
                // the connection was never started so there is nothing to clean up
                return;
            }

            try
            {
                await _startTcs.Task;
            }
            catch
            {
                // We only await the start task to make sure that StartAsync completed. The
                // _startTask is returned to the user and they should handle exceptions.
            }

            if (_transportChannel != null)
            {
                Output.TryComplete();
            }

            if (_transport != null)
            {
                await _transport.StopAsync();
            }

            if (_receiveLoopTask != null)
            {
                await _receiveLoopTask;
            }

            _httpClient.Dispose();
        }

        private class ConnectionState
        {
            public const int Initial = 0;
            public const int Connecting = 1;
            public const int Connected = 2;
            public const int Disconnected = 3;
        }

        private class NegotiationResponse
        {
            public string ConnectionId { get; set; }
            public TransportType[] AvailableTransports { get; set; }
        }
    }
}
