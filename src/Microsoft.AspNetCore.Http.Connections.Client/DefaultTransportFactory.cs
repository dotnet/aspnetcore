// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    public class DefaultTransportFactory : ITransportFactory
    {
        private readonly HttpClient _httpClient;
        private readonly HttpOptions _httpOptions;
        private readonly HttpTransportType _requestedTransportType;
        private readonly ILoggerFactory _loggerFactory;
        private static volatile bool _websocketsSupported = true;

        public DefaultTransportFactory(HttpTransportType requestedTransportType, ILoggerFactory loggerFactory, HttpClient httpClient, HttpOptions httpOptions)
        {
            if (httpClient == null && requestedTransportType != HttpTransportType.WebSockets)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            _requestedTransportType = requestedTransportType;
            _loggerFactory = loggerFactory;
            _httpClient = httpClient;
            _httpOptions = httpOptions;
        }

        public ITransport CreateTransport(HttpTransportType availableServerTransports)
        {
            if (_websocketsSupported && (availableServerTransports & HttpTransportType.WebSockets & _requestedTransportType) == HttpTransportType.WebSockets)
            {
                try
                {
                    return new WebSocketsTransport(_httpOptions, _loggerFactory);
                }
                catch (PlatformNotSupportedException)
                {
                    _websocketsSupported = false;
                }
            }

            if ((availableServerTransports & HttpTransportType.ServerSentEvents & _requestedTransportType) == HttpTransportType.ServerSentEvents)
            {
                return new ServerSentEventsTransport(_httpClient, _loggerFactory);
            }

            if ((availableServerTransports & HttpTransportType.LongPolling & _requestedTransportType) == HttpTransportType.LongPolling)
            {
                return new LongPollingTransport(_httpClient, _loggerFactory);
            }

            throw new InvalidOperationException("No requested transports available on the server.");
        }
    }
}
