// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public class DefaultTransportFactory : ITransportFactory
    {
        private readonly HttpClient _httpClient;
        private readonly TransportType _requestedTransportType;
        private readonly ILoggerFactory _loggerFactory;
        private static volatile bool _websocketsSupported = true;

        public DefaultTransportFactory(TransportType requestedTransportType, ILoggerFactory loggerFactory, HttpClient httpClient)
        {
            if (requestedTransportType <= 0 || requestedTransportType > TransportType.All)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedTransportType));
            }

            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            _requestedTransportType = requestedTransportType;
            _loggerFactory = loggerFactory;
            _httpClient = httpClient;
        }

        public ITransport CreateTransport(TransportType availableServerTransports)
        {
            if (_websocketsSupported && (availableServerTransports & TransportType.WebSockets & _requestedTransportType) == TransportType.WebSockets)
            {
                try
                {
                    return new WebSocketsTransport(_loggerFactory);
                }
                catch (PlatformNotSupportedException)
                {
                    _websocketsSupported = false;
                }
            }

            if ((availableServerTransports & TransportType.ServerSentEvents & _requestedTransportType) == TransportType.ServerSentEvents)
            {
                return new ServerSentEventsTransport(_httpClient, _loggerFactory);
            }

            if ((availableServerTransports & TransportType.LongPolling & _requestedTransportType) == TransportType.LongPolling)
            {
                return new LongPollingTransport(_httpClient, _loggerFactory);
            }

            throw new InvalidOperationException("No requested transports available on the server.");
        }
    }
}
