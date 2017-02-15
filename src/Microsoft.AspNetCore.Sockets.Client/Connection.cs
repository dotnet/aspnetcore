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
        private IChannelConnection<Message> _transportChannel;
        private ITransport _transport;
        private readonly ILogger _logger;

        public Uri Url { get; }

        private Connection(Uri url, ITransport transport, IChannelConnection<Message> transportChannel, ILogger logger)
        {
            Url = url;

            _logger = logger;
            _transport = transport;
            _transportChannel = transportChannel;
        }

        private ReadableChannel<Message> Input => _transportChannel.Input;
        private WritableChannel<Message> Output => _transportChannel.Output;

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
            Output.TryComplete();
            await _transport.StopAsync();
            await DrainMessages();
        }

        public void Dispose()
        {
            Output.TryComplete();
            _transport.Dispose();
        }

        private async Task DrainMessages()
        {
            while (await Input.WaitToReadAsync())
            {
                if (Input.TryRead(out Message message))
                {
                    message.Dispose();
                }
            }
        }

        public static Task<Connection> ConnectAsync(Uri url, ITransport transport) => ConnectAsync(url, transport, null, null);
        public static Task<Connection> ConnectAsync(Uri url, ITransport transport, ILoggerFactory loggerFactory) => ConnectAsync(url, transport, null, loggerFactory);
        public static Task<Connection> ConnectAsync(Uri url, ITransport transport, HttpClient httpClient) => ConnectAsync(url, transport, httpClient, null);

        public static async Task<Connection> ConnectAsync(Uri url, ITransport transport, HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            // TODO: Once we have websocket transport we would be able to use it as the default transport
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger<Connection>();

            var connectUrl = await GetConnectUrl(url, httpClient, logger);

            var applicationToTransport = Channel.CreateUnbounded<Message>();
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);
            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);


            // Start the transport, giving it one end of the pipeline
            try
            {
                await transport.StartAsync(connectUrl, applicationSide);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to start connection. Error starting transport '{0}': {1}", transport.GetType().Name, ex);
                throw;
            }

            // Create the connection, giving it the other end of the pipeline
            return new Connection(url, transport, transportSide, logger);
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
    }
}
