// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class Connection : IChannelConnection<Message>
    {
        private IChannelConnection<Message> _transportChannel;
        private ITransport _transport;
        private readonly ILogger _logger;

        public Uri Url { get; }

        // TODO: Review. This is really only designed to be used from ConnectAsync
        private Connection(Uri url, ITransport transport, IChannelConnection<Message> transportChannel, ILogger logger)
        {
            Url = url;

            _logger = logger;
            _transport = transport;
            _transportChannel = transportChannel;
        }

        public ReadableChannel<Message> Input => _transportChannel.Input;
        public WritableChannel<Message> Output => _transportChannel.Output;

        public void Dispose()
        {
            _transport.Dispose();
        }

        public static Task<Connection> ConnectAsync(Uri url, ITransport transport) => ConnectAsync(url, transport, new HttpClient(), NullLoggerFactory.Instance);
        public static Task<Connection> ConnectAsync(Uri url, ITransport transport, ILoggerFactory loggerFactory) => ConnectAsync(url, transport, new HttpClient(), loggerFactory);
        public static Task<Connection> ConnectAsync(Uri url, ITransport transport, HttpClient httpClient) => ConnectAsync(url, transport, httpClient, NullLoggerFactory.Instance);

        public static async Task<Connection> ConnectAsync(Uri url, ITransport transport, HttpClient httpClient, ILoggerFactory loggerFactory)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }

            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            var logger = loggerFactory.CreateLogger<Connection>();
            var negotiateUrl = Utils.AppendPath(url, "negotiate");

            string connectionId;
            try
            {
                // Get a connection ID from the server
                logger.LogDebug("Establishing Connection at: {0}", negotiateUrl);
                connectionId = await httpClient.GetStringAsync(negotiateUrl);
                logger.LogDebug("Connection Id: {0}", connectionId);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to start connection. Error getting connection id from '{0}': {1}", negotiateUrl, ex);
                throw;
            }

            var connectedUrl = Utils.AppendQueryString(url, "id=" + connectionId);

            var applicationToTransport = Channel.CreateUnbounded<Message>();
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);
            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);


            // Start the transport, giving it one end of the pipeline
            try
            {
                await transport.StartAsync(connectedUrl, applicationSide);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to start connection. Error starting transport '{0}': {1}", transport.GetType().Name, ex);
                throw;
            }

            // Create the connection, giving it the other end of the pipeline
            return new Connection(url, transport, transportSide, logger);
        }
    }
}
