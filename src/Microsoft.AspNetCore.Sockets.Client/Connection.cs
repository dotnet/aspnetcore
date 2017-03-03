// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class Connection: IConnection
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private volatile int _connectionState = ConnectionState.Initial;
        private volatile IChannelConnection<Message, SendMessage> _transportChannel;
        private volatile ITransport _transport;
        private volatile Task _receiveLoopTask;
        private volatile Task _startTask = Task.CompletedTask;
        private TaskQueue _eventQueue = new TaskQueue();

        private ReadableChannel<Message> Input => _transportChannel.Input;
        private WritableChannel<SendMessage> Output => _transportChannel.Output;

        public Uri Url { get; }

        public event Action Connected;
        public event Action<byte[], MessageType> Received;
        public event Action<Exception> Closed;

        public Connection(Uri url)
            : this(url, null)
        { }

        public Connection(Uri url, ILoggerFactory loggerFactory)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));

            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<Connection>();
        }

        public Task StartAsync() => StartAsync(transport: null, httpClient: null);
        public Task StartAsync(HttpClient httpClient) => StartAsync(transport: null, httpClient: httpClient);
        public Task StartAsync(ITransport transport) => StartAsync(transport: transport, httpClient: null);

        public Task StartAsync(ITransport transport, HttpClient httpClient)
        {
            _startTask = StartAsyncInternal(transport, httpClient);
            return _startTask;
        }

        private async Task StartAsyncInternal(ITransport transport, HttpClient httpClient)
        {
            if (Interlocked.CompareExchange(ref _connectionState, ConnectionState.Connecting, ConnectionState.Initial)
                != ConnectionState.Initial)
            {
                throw new InvalidOperationException("Cannot start a connection that is not in the Initial state.");
            }

            try
            {
                var connectUrl = await GetConnectUrl(Url, httpClient, _logger);

                // Connection is being stopped while start was in progress
                if (_connectionState == ConnectionState.Disconnected)
                {
                    return;
                }

                _transport = transport ?? new WebSocketsTransport(_loggerFactory);
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
                    // Do not "simplify" - events can be removed from a different thread
                    var connectedEventHandler = Connected;
                    if (connectedEventHandler != null)
                    {
                        connectedEventHandler();
                    }

                    return Task.CompletedTask;
                });

                ignore = Input.Completion.ContinueWith(async t =>
                {
                    Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected);

                    await _eventQueue.Drain();

                    // Do not "simplify" - event handlers can be removed from a different thread
                    var closedEventHandler = Closed;
                    if (closedEventHandler != null)
                    {
                        closedEventHandler(t.IsFaulted ? t.Exception.InnerException : null);
                    }

                    return Task.CompletedTask;
                });

                // start receive loop only after the Connected event was raised to
                // avoid Received event being raised before the Connected event
                _receiveLoopTask = ReceiveAsync();
            }
        }

        private static async Task<Uri> GetConnectUrl(Uri url, HttpClient httpClient, ILogger logger)
        {
            var disposeHttpClient = httpClient == null;
            httpClient = httpClient ?? new HttpClient();
            try
            {
                var connectionId = await GetConnectionId(url, httpClient, logger);
                return Utils.AppendQueryString(url, "id=" + connectionId);
            }
            finally
            {
                if (disposeHttpClient)
                {
                    httpClient.Dispose();
                }
            }
        }

        private static async Task<string> GetConnectionId(Uri url, HttpClient httpClient, ILogger logger)
        {
            var negotiateUrl = Utils.AppendPath(url, "negotiate");
            try
            {
                // Get a connection ID from the server
                logger.LogDebug("Establishing Connection at: {0}", negotiateUrl);
                var connectionId = await httpClient.GetStringAsync(negotiateUrl);
                logger.LogDebug("Connection Id: {0}", connectionId);
                return connectionId;
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to start connection. Error getting connection id from '{0}': {1}", negotiateUrl, ex);
                throw;
            }
        }

        private async Task StartTransport(Uri connectUrl)
        {
            var applicationToTransport = Channel.CreateUnbounded<SendMessage>();
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationSide = new ChannelConnection<SendMessage, Message>(applicationToTransport, transportToApplication);

            _transportChannel = new ChannelConnection<Message, SendMessage>(transportToApplication, applicationToTransport);

            // Start the transport, giving it one end of the pipeline
            try
            {
                await _transport.StartAsync(connectUrl, applicationSide);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start connection. Error starting transport '{0}': {1}", _transport.GetType().Name, ex);
                throw;
            }
        }

        private async Task ReceiveAsync()
        {
            try
            {
                _logger.LogTrace("Beginning receive loop.");

                while (await Input.WaitToReadAsync())
                {
                    if (_connectionState != ConnectionState.Connected)
                    {
                        _logger.LogDebug("Message received but connection is not connected. Skipping raising Received event.");
                        // drain
                        Input.TryRead(out Message ignore);
                        continue;
                    }

                    if (Input.TryRead(out Message message))
                    {
                        _logger.LogDebug("Scheduling raising Received event.");
                        var ignore = _eventQueue.Enqueue(() => 
                        {
                            // Do not "simplify" - event handlers can be removed from a different thread
                            var receivedEventHandler = Received;
                            if (receivedEventHandler != null)
                            {
                                receivedEventHandler(message.Payload, message.Type);
                            }

                            return Task.CompletedTask;
                        });
                    }
                    else
                    {
                        _logger.LogDebug("Could not read message.");
                    }
                }

                await Input.Completion;
            }
            catch (Exception ex)
            {
                Output.TryComplete(ex);
                _logger.LogError("Error receiving message: {0}", ex);
            }

            _logger.LogTrace("Ending receive loop");
        }

        public Task SendAsync(byte[] data, MessageType type)
        {
            return SendAsync(data, type, CancellationToken.None);
        }

        public async Task SendAsync(byte[] data, MessageType type, CancellationToken cancellationToken)
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
            var message = new SendMessage(data, type, sendTcs);

            while (await Output.WaitToWriteAsync(cancellationToken))
            {
                if (Output.TryWrite(message))
                {
                    await sendTcs.Task;
                    break;
                }
            }
        }

        public async Task DisposeAsync()
        {
            _logger.LogInformation("Stopping client.");

            Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected);
            try
            {
                await _startTask;
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
        }

        private class ConnectionState
        {
            public const int Initial = 0;
            public const int Connecting = 1;
            public const int Connected = 2;
            public const int Disconnected = 3;
        }
    }
}
