// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.AspNetCore.Sockets.Http.Internal;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class HttpConnection : IConnection
    {
        private static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(120);

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private volatile ConnectionState _connectionState = ConnectionState.Disconnected;
        private readonly object _stateChangeLock = new object();

        private volatile ChannelConnection<byte[], SendMessage> _transportChannel;
        private readonly HttpClient _httpClient;
        private readonly HttpOptions _httpOptions;
        private volatile ITransport _transport;
        private volatile Task _receiveLoopTask;
        private TaskCompletionSource<object> _startTcs;
        private TaskCompletionSource<object> _closeTcs;
        private TaskQueue _eventQueue;
        private readonly ITransportFactory _transportFactory;
        private string _connectionId;
        private Exception _abortException;
        private readonly TimeSpan _eventQueueDrainTimeout = TimeSpan.FromSeconds(5);
        private ChannelReader<byte[]> Input => _transportChannel.Input;
        private ChannelWriter<SendMessage> Output => _transportChannel.Output;
        private readonly List<ReceiveCallback> _callbacks = new List<ReceiveCallback>();
        private readonly TransportType _requestedTransportType = TransportType.All;
        private readonly ConnectionLogScope _logScope;
        private readonly IDisposable _scopeDisposable;

        public Uri Url { get; }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public event Action<Exception> Closed;

        public HttpConnection(Uri url)
            : this(url, TransportType.All)
        { }

        public HttpConnection(Uri url, TransportType transportType)
            : this(url, transportType, loggerFactory: null)
        {
        }

        public HttpConnection(Uri url, ILoggerFactory loggerFactory)
            : this(url, TransportType.All, loggerFactory, httpOptions: null)
        {
        }

        public HttpConnection(Uri url, TransportType transportType, ILoggerFactory loggerFactory)
            : this(url, transportType, loggerFactory, httpOptions: null)
        {
        }

        public HttpConnection(Uri url, TransportType transportType, ILoggerFactory loggerFactory, HttpOptions httpOptions)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));

            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<HttpConnection>();
            _httpOptions = httpOptions;

            _requestedTransportType = transportType;
            if (_requestedTransportType != TransportType.WebSockets)
            {
                _httpClient = httpOptions?.HttpMessageHandler == null ? new HttpClient() : new HttpClient(httpOptions.HttpMessageHandler);
                _httpClient.Timeout = HttpClientTimeout;
            }

            _transportFactory = new DefaultTransportFactory(transportType, _loggerFactory, _httpClient, httpOptions);
            _logScope = new ConnectionLogScope();
            _scopeDisposable = _logger.BeginScope(_logScope);
        }

        public HttpConnection(Uri url, ITransportFactory transportFactory, ILoggerFactory loggerFactory, HttpOptions httpOptions)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<HttpConnection>();
            _httpOptions = httpOptions;
            _httpClient = _httpOptions?.HttpMessageHandler == null ? new HttpClient() : new HttpClient(_httpOptions?.HttpMessageHandler);
            _httpClient.Timeout = HttpClientTimeout;
            _transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
            _logScope = new ConnectionLogScope();
            _scopeDisposable = _logger.BeginScope(_logScope);
        }

        public async Task StartAsync() => await StartAsyncCore().ForceAsync();

        private Task StartAsyncCore()
        {
            if (ChangeState(from: ConnectionState.Disconnected, to: ConnectionState.Connecting) != ConnectionState.Disconnected)
            {
                return Task.FromException(
                    new InvalidOperationException($"Cannot start a connection that is not in the {nameof(ConnectionState.Disconnected)} state."));
            }

            _startTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _eventQueue = new TaskQueue();

            StartAsyncInternal()
                .ContinueWith(t =>
                {
                    var abortException = _abortException;
                    if (t.IsFaulted || abortException != null)
                    {
                        _startTcs.SetException(_abortException ?? t.Exception.InnerException);
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
                var connectUrl = Url;
                if (_requestedTransportType == TransportType.WebSockets)
                {
                    _transport = _transportFactory.CreateTransport(TransportType.WebSockets);
                }
                else
                {
                    var negotiationResponse = await Negotiate(Url, _httpClient, _logger);
                    _connectionId = negotiationResponse.ConnectionId;
                    _logScope.ConnectionId = _connectionId;

                    // Connection is being disposed while start was in progress
                    if (_connectionState == ConnectionState.Disposed)
                    {
                        _logger.HttpConnectionClosed();
                        return;
                    }

                    _transport = _transportFactory.CreateTransport(GetAvailableServerTransports(negotiationResponse));
                    connectUrl = CreateConnectUrl(Url, negotiationResponse);
                }

                _logger.StartingTransport(_transport, connectUrl);
                await StartTransport(connectUrl);
            }
            catch
            {
                // The connection can now be either in the Connecting or Disposed state - only change the state to
                // Disconnected if the connection was in the Connecting state to not resurrect a Disposed connection
                ChangeState(from: ConnectionState.Connecting, to: ConnectionState.Disconnected);
                throw;
            }

            // if the connection is not in the Connecting state here it means the user called DisposeAsync while
            // the connection was starting
            if (ChangeState(from: ConnectionState.Connecting, to: ConnectionState.Connected) == ConnectionState.Connecting)
            {
                _closeTcs = new TaskCompletionSource<object>();

                _ = Input.Completion.ContinueWith(async t =>
                {
                    // Grab the exception and then clear it.
                    // See comment at AbortAsync for more discussion on the thread-safety
                    // StartAsync can't be called until the ChangeState below, so we're OK.
                    var abortException = _abortException;
                    _abortException = null;

                    // There is an inherent race between receive and close. Removing the last message from the channel
                    // makes Input.Completion task completed and runs this continuation. We need to await _receiveLoopTask
                    // to make sure that the message removed from the channel is processed before we drain the queue.
                    // There is a short window between we start the channel and assign the _receiveLoopTask a value.
                    // To make sure that _receiveLoopTask can be awaited (i.e. is not null) we need to await _startTask.
                    _logger.ProcessRemainingMessages();

                    await _startTcs.Task;
                    await _receiveLoopTask;

                    _logger.DrainEvents();

                    await Task.WhenAny(_eventQueue.Drain().NoThrow(), Task.Delay(_eventQueueDrainTimeout));

                    _logger.CompleteClosed();
                    _logScope.ConnectionId = null;

                    // At this point the connection can be either in the Connected or Disposed state. The state should be changed
                    // to the Disconnected state only if it was in the Connected state.
                    // From this point on, StartAsync can be called at any time.
                    ChangeState(from: ConnectionState.Connected, to: ConnectionState.Disconnected);

                    _closeTcs.SetResult(null);

                    try
                    {
                        if (t.IsFaulted)
                        {
                            Closed?.Invoke(t.Exception.InnerException);
                        }
                        else
                        {
                            // Call the closed event. If there was an abort exception, it will be flowed forward
                            // However, if there wasn't, this will just be null and we're good
                            Closed?.Invoke(abortException);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Suppress (but log) the exception, this is user code
                        _logger.ErrorDuringClosedEvent(ex);
                    }
                });

                _receiveLoopTask = ReceiveAsync();
            }
        }

        private async Task<NegotiationResponse> Negotiate(Uri url, HttpClient httpClient, ILogger logger)
        {
            try
            {
                // Get a connection ID from the server
                logger.EstablishingConnection(url);
                var urlBuilder = new UriBuilder(url);
                if (!urlBuilder.Path.EndsWith("/"))
                {
                    urlBuilder.Path += "/";
                }
                urlBuilder.Path += "negotiate";

                using (var request = new HttpRequestMessage(HttpMethod.Post, urlBuilder.Uri))
                {
                    SendUtils.PrepareHttpRequest(request, _httpOptions);

                    using (var response = await httpClient.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        return await ParseNegotiateResponse(response, logger);
                    }
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
                await _transport.StartAsync(connectUrl, applicationSide, GetTransferMode(), this);

                // actual transfer mode can differ from the one that was requested so set it on the feature
                if (!_transport.Mode.HasValue)
                {
                    // This can happen with custom transports so it should be an exception, not an assert.
                    throw new InvalidOperationException("Transport was expected to set the Mode property after StartAsync, but it has not been set.");
                }
                SetTransferMode(_transport.Mode.Value);
            }
            catch (Exception ex)
            {
                _logger.ErrorStartingTransport(_transport, ex);
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
                _logger.HttpReceiveStarted();

                while (await Input.WaitToReadAsync())
                {
                    if (_connectionState != ConnectionState.Connected)
                    {
                        _logger.SkipRaisingReceiveEvent();
                        // drain
                        Input.TryRead(out _);
                        continue;
                    }

                    if (Input.TryRead(out var buffer))
                    {
                        _logger.ScheduleReceiveEvent();
                        _ = _eventQueue.Enqueue(async () =>
                        {
                            _logger.RaiseReceiveEvent();

                            // Copying the callbacks to avoid concurrency issues
                            ReceiveCallback[] callbackCopies;
                            lock (_callbacks)
                            {
                                callbackCopies = new ReceiveCallback[_callbacks.Count];
                                _callbacks.CopyTo(callbackCopies);
                            }

                            foreach (var callbackObject in callbackCopies)
                            {
                                try
                                {
                                    await callbackObject.InvokeAsync(buffer);
                                }
                                catch (Exception ex)
                                {
                                    _logger.ExceptionThrownFromCallback(nameof(OnReceived), ex);
                                }
                            }
                        });
                    }
                    else
                    {
                        _logger.FailedReadingMessage();
                    }
                }

                await Input.Completion;
            }
            catch (Exception ex)
            {
                Output.TryComplete(ex);
                _logger.ErrorReceiving(ex);
            }

            _logger.EndReceive();
        }

        public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default) =>
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

            _logger.SendingMessage();

            while (await Output.WaitToWriteAsync(cancellationToken))
            {
                if (Output.TryWrite(message))
                {
                    await sendTcs.Task;
                    break;
                }
            }
        }

        // AbortAsync creates a few thread-safety races that we are OK with.
        //  1. If the transport shuts down gracefully after AbortAsync is called but BEFORE _abortException is called, then the
        //     Closed event will not receive the Abort exception. This is OK because technically the transport was shut down gracefully
        //     before it was aborted
        //  2. If the transport is closed gracefully and then AbortAsync is called before it captures the _abortException value
        //     the graceful shutdown could be turned into an abort. However, again, this is an inherent race between two different conditions:
        //     The transport shutting down because the server went away, and the user requesting the Abort
        //  3. Finally, because this is an instance field, there is a possible race around accidentally re-using _abortException in the restarted
        //     connection. The scenario here is: AbortAsync(someException); StartAsync(); CloseAsync(); Where the _abortException value from the
        //     first AbortAsync call is still set at the time CloseAsync gets to calling the Closed event. However, this can't happen because the
        //     StartAsync method can't be called until the connection state is changed to Disconnected, which happens AFTER the close code
        //     captures and resets _abortException.
        public async Task AbortAsync(Exception exception) => await StopAsyncCore(exception ?? throw new ArgumentNullException(nameof(exception))).ForceAsync();

        public async Task StopAsync() => await StopAsyncCore(exception: null).ForceAsync();

        private async Task StopAsyncCore(Exception exception)
        {
            lock (_stateChangeLock)
            {
                if (!(_connectionState == ConnectionState.Connecting || _connectionState == ConnectionState.Connected))
                {
                    _logger.SkippingStop();
                    return;
                }
            }

            // Note that this method can be called at the same time when the connection is being closed from the server
            // side due to an error. We are resilient to this since we merely try to close the channel here and the
            // channel can be closed only once. As a result the continuation that does actual job and raises the Closed
            // event runs always only once.

            // See comment at AbortAsync for more discussion on the thread-safety of this.
            _abortException = exception;

            _logger.StoppingClient();

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

            if (_closeTcs != null)
            {
                await _closeTcs.Task;
            }
        }

        public async Task DisposeAsync() => await DisposeAsyncCore().ForceAsync();

        private async Task DisposeAsyncCore()
        {
            // This will no-op if we're already stopped
            await StopAsyncCore(exception: null);

            if (ChangeState(to: ConnectionState.Disposed) == ConnectionState.Disposed)
            {
                _logger.SkippingDispose();

                // the connection was already disposed
                return;
            }

            _logger.DisposingClient();

            _httpClient?.Dispose();
            _scopeDisposable.Dispose();
        }

        public IDisposable OnReceived(Func<byte[], object, Task> callback, object state)
        {
            var receiveCallback = new ReceiveCallback(callback, state);
            lock (_callbacks)
            {
                _callbacks.Add(receiveCallback);
            }
            return new Subscription(receiveCallback, _callbacks);
        }

        private class ReceiveCallback
        {
            private readonly Func<byte[], object, Task> _callback;
            private readonly object _state;

            public ReceiveCallback(Func<byte[], object, Task> callback, object state)
            {
                _callback = callback;
                _state = state;
            }

            public Task InvokeAsync(byte[] data)
            {
                return _callback(data, _state);
            }
        }

        private class Subscription : IDisposable
        {
            private readonly ReceiveCallback _receiveCallback;
            private readonly List<ReceiveCallback> _callbacks;
            public Subscription(ReceiveCallback callback, List<ReceiveCallback> callbacks)
            {
                _receiveCallback = callback;
                _callbacks = callbacks;
            }

            public void Dispose()
            {
                lock (_callbacks)
                {
                    _callbacks.Remove(_receiveCallback);
                }
            }
        }

        private ConnectionState ChangeState(ConnectionState from, ConnectionState to)
        {
            lock (_stateChangeLock)
            {
                var state = _connectionState;
                if (_connectionState == from)
                {
                    _connectionState = to;
                }

                _logger.ConnectionStateChanged(state, to);
                return state;
            }
        }

        private ConnectionState ChangeState(ConnectionState to)
        {
            lock (_stateChangeLock)
            {
                var state = _connectionState;
                _connectionState = to;
                _logger.ConnectionStateChanged(state, to);
                return state;
            }
        }

        // Internal because it's used by logging to avoid ToStringing prematurely.
        internal enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Disposed
        }

        private class NegotiationResponse
        {
            public string ConnectionId { get; set; }
            public TransportType[] AvailableTransports { get; set; }
        }
    }
}
