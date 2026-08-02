// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed partial class DefaultTransportFactory : ITransportFactory
{
    private readonly HttpClient? _httpClient;
    private readonly HttpConnectionOptions _httpConnectionOptions;
    private readonly Func<Task<string?>> _accessTokenProvider;
    private readonly HttpTransportType _requestedTransportType;
    private readonly ILoggerFactory _loggerFactory;
    private static volatile bool _websocketsSupported = true;

    public DefaultTransportFactory(HttpTransportType requestedTransportType, ILoggerFactory loggerFactory, HttpClient? httpClient, HttpConnectionOptions httpConnectionOptions, Func<Task<string?>> accessTokenProvider)
    {
        if (httpClient == null && requestedTransportType != HttpTransportType.WebSockets)
        {
            throw new ArgumentException($"{nameof(httpClient)} cannot be null when {nameof(requestedTransportType)} is not {nameof(HttpTransportType.WebSockets)}.", nameof(httpClient));
        }

        _requestedTransportType = requestedTransportType;
        _loggerFactory = loggerFactory;
        _httpClient = httpClient;
        _httpConnectionOptions = httpConnectionOptions;
        _accessTokenProvider = accessTokenProvider;
    }

    public ITransport CreateTransport(HttpTransportType availableServerTransports, bool useStatefulReconnect)
    {
        if (_websocketsSupported && (availableServerTransports & HttpTransportType.WebSockets & _requestedTransportType) == HttpTransportType.WebSockets)
        {
            try
            {
                return new WebSocketsTransport(_httpConnectionOptions, _loggerFactory, _accessTokenProvider, _httpClient, useStatefulReconnect);
            }
            catch (PlatformNotSupportedException ex)
            {
                Log.TransportNotSupported(_loggerFactory.CreateLogger<DefaultTransportFactory>(), HttpTransportType.WebSockets, ex);
                _websocketsSupported = false;
            }
        }

        if ((availableServerTransports & HttpTransportType.ServerSentEvents & _requestedTransportType) == HttpTransportType.ServerSentEvents)
        {
            // We don't need to give the transport the accessTokenProvider because the HttpClient has a message handler that does the work for us.
            return new ServerSentEventsTransport(_httpClient!, _httpConnectionOptions, _loggerFactory);
        }

        if ((availableServerTransports & HttpTransportType.LongPolling & _requestedTransportType) == HttpTransportType.LongPolling)
        {
            // We don't need to give the transport the accessTokenProvider because the HttpClient has a message handler that does the work for us.
            return new LongPollingTransport(_httpClient!, _httpConnectionOptions, _loggerFactory);
        }

        throw new InvalidOperationException("No requested transports available on the server.");
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Transport '{TransportType}' is not supported.", EventName = "TransportNotSupported")]
        public static partial void TransportNotSupported(ILogger logger, HttpTransportType transportType, Exception ex);
    }
}
