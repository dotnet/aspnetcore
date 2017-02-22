// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class Connection: IConnection
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private int _connectionState = ConnectionState.Initial;
        private IChannelConnection<Message> _transportChannel;
        private ITransport _transport;
        private Task _receiveLoopTask;

        private ReadableChannel<Message> Input => _transportChannel.Input;
        private WritableChannel<Message> Output => _transportChannel.Output;

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

        // TODO HIGH: Fix a race when the connection is being stopped/disposed when start has not finished running
        public async Task StartAsync(ITransport transport, HttpClient httpClient)
        {
            _transport = transport ?? new WebSocketsTransport(_loggerFactory);

            if (Interlocked.CompareExchange(ref _connectionState, ConnectionState.Connecting, ConnectionState.Initial)
                != ConnectionState.Initial)
            {
                throw new InvalidOperationException("Cannot start a connection that is not in the Initial state.");
            }

            try
            {
                var connectUrl = await GetConnectUrl(Url, httpClient, _logger);
                await StartTransport(connectUrl);
            }
            catch
            {
                Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected);
                throw;
            }

            // start receive loop
            _receiveLoopTask = ReceiveAsync();

            Interlocked.Exchange(ref _connectionState, ConnectionState.Connected);

            // Do not "simplify" - events can be removed from a different thread
            var connectedEventHandler = Connected;
            if (connectedEventHandler != null)
            {
                connectedEventHandler();
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
            var applicationToTransport = Channel.CreateUnbounded<Message>();
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            _transportChannel = new ChannelConnection<Message>(applicationToTransport, transportToApplication);

            var ignore = Input.Completion.ContinueWith(t =>
            {
                Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected);

                // Do not "simplify" - events can be removed from a different thread
                var closedEventHandler = Closed;
                if (closedEventHandler != null)
                {
                    closedEventHandler(t.IsFaulted ? t.Exception.InnerException : null);
                }
            });

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
                _logger.LogTrace("Beginning receive loop");

                while (await Input.WaitToReadAsync())
                {
                    if (Input.TryRead(out Message message))
                    {
                        // Do not "simplify" - events can be removed from a different thread
                        var receivedEventHandler = Received;
                        if (receivedEventHandler != null)
                        {
                            receivedEventHandler(message.Payload, message.Type);
                        }
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

        public Task<bool> SendAsync(byte[] data, MessageType type)
        {
            return SendAsync(data, type, CancellationToken.None);
        }

        public async Task<bool> SendAsync(byte[] data, MessageType type, CancellationToken cancellationToken)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_connectionState != ConnectionState.Connected)
            {
                return false;
            }

            var message = new Message(data, type);

            while (await Output.WaitToWriteAsync(cancellationToken))
            {
                if (Output.TryWrite(message))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task DisposeAsync()
        {
            Interlocked.Exchange(ref _connectionState, ConnectionState.Disconnected);

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
