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
    public class Connection : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private IChannelConnection<Message> _transportChannel;
        private ITransport _transport;

        private ReadableChannel<Message> Input => _transportChannel.Input;
        private WritableChannel<Message> Output => _transportChannel.Output;

        public Uri Url { get; }

        public Connection(Uri url)
            : this(url, null)
        { }

        public Connection(Uri url, ILoggerFactory loggerFactory)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));

            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<Connection>();
        }

        public Task StartAsync(Uri url, ITransport transport) => StartAsync((ITransport)null, null);
        public Task StartAsync(HttpClient httpClient) => StartAsync(null, httpClient);
        public Task StartAsync(ITransport transport) => StartAsync(transport, null);

        public async Task StartAsync(ITransport transport, HttpClient httpClient)
        {
            // TODO: make transport optional
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));

            var connectUrl = await GetConnectUrl(Url, httpClient, _logger);

            var applicationToTransport = Channel.CreateUnbounded<Message>();
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);
            _transportChannel = new ChannelConnection<Message>(applicationToTransport, transportToApplication);


            // Start the transport, giving it one end of the pipeline
            try
            {
                await transport.StartAsync(connectUrl, applicationSide);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start connection. Error starting transport '{0}': {1}", transport.GetType().Name, ex);
                throw;
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

        public Task<bool> ReceiveAsync(ReceiveData receiveData)
        {
            return ReceiveAsync(receiveData, CancellationToken.None);
        }

        public async Task<bool> ReceiveAsync(ReceiveData receiveData, CancellationToken cancellationToken)
        {
            if (receiveData == null)
            {
                throw new ArgumentNullException(nameof(receiveData));
            }

            if (Input.Completion.IsCompleted)
            {
                throw new InvalidOperationException("Cannot receive messages when the connection is stopped.");
            }

            try
            {
                while (await Input.WaitToReadAsync(cancellationToken))
                {
                    if (Input.TryRead(out Message message))
                    {
                        using (message)
                        {
                            receiveData.MessageType = message.Type;
                            receiveData.Data = message.Payload.Buffer.ToArray();
                            return true;
                        }
                    }
                }

                await Input.Completion;
            }
            catch (OperationCanceledException)
            {
                // channel is being closed
            }
            catch (Exception ex)
            {
                Output.TryComplete(ex);
                _logger.LogError("Error receiving message: {0}", ex);
                throw;
            }

            return false;
        }

        public Task<bool> SendAsync(byte[] data, MessageType type)
        {
            return SendAsync(data, type, CancellationToken.None);
        }

        public async Task<bool> SendAsync(byte[] data, MessageType type, CancellationToken cancellationToken)
        {
            var message = new Message(ReadableBuffer.Create(data).Preserve(), type);

            while (await Output.WaitToWriteAsync(cancellationToken))
            {
                if (Output.TryWrite(message))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task StopAsync()
        {
            if (_transportChannel != null)
            {
                Output.TryComplete();
            }

            if (_transport != null)
            {
                await _transport.StopAsync();
            }
        }

        public void Dispose()
        {
            if (_transportChannel != null)
            {
                Output.TryComplete();
            }

            if (_transport != null)
            {
                _transport.Dispose();
            }
        }
    }
}
