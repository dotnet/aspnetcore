// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    /// <summary>
    /// Used to make a connection to an ASP.NET Core ConnectionHandler using an HTTP-based transport.
    /// </summary>
    public partial class HttpConnection : ConnectionContext, IConnectionInherentKeepAliveFeature
    {
        // Not configurable on purpose, high enough that if we reach here, it's likely
        // a buggy server
        private static readonly int _maxRedirects = 100;
        private static readonly Task<string> _noAccessToken = Task.FromResult<string>(null);

        private static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(120);
#if !NETCOREAPP3_0
        private static readonly Version Windows8Version = new Version(6, 2);
#endif

        private readonly ILogger _logger;

        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private bool _started;
        private bool _disposed;
        private bool _hasInherentKeepAlive;

        private readonly HttpClient _httpClient;
        private readonly HttpConnectionOptions _httpConnectionOptions;
        private ITransport _transport;
        private readonly ITransportFactory _transportFactory;
        private string _connectionId;
        private readonly ConnectionLogScope _logScope;
        private readonly ILoggerFactory _loggerFactory;
        private Func<Task<string>> _accessTokenProvider;

        /// <inheritdoc />
        public override IDuplexPipe Transport
        {
            get
            {
                CheckDisposed();
                if (_transport == null)
                {
                    throw new InvalidOperationException($"Cannot access the {nameof(Transport)} pipe before the connection has started.");
                }
                return _transport;
            }
            set => throw new NotSupportedException("The transport pipe isn't settable.");
        }

        /// <inheritdoc />
        public override IFeatureCollection Features { get; } = new FeatureCollection();

        /// <summary>
        /// Gets or sets the connection ID.
        /// </summary>
        /// <remarks>
        /// The connection ID is set when the <see cref="HttpConnection"/> is started and should not be set by user code.
        /// If the connection was created with <see cref="HttpConnectionOptions.SkipNegotiation"/> set to <c>true</c>
        /// then the connection ID will be <c>null</c>.
        /// </remarks>
        public override string ConnectionId
        {
            get => _connectionId;
            set => throw new InvalidOperationException("The ConnectionId is set internally and should not be set by user code.");
        }

        /// <inheritdoc />
        public override IDictionary<object, object> Items { get; set; } = new ConnectionItems();

        /// <inheritdoc />
        bool IConnectionInherentKeepAliveFeature.HasInherentKeepAlive => _hasInherentKeepAlive;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnection"/> class.
        /// </summary>
        /// <param name="url">The URL to connect to.</param>
        public HttpConnection(Uri url)
            : this(url, HttpTransports.All)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnection"/> class.
        /// </summary>
        /// <param name="url">The URL to connect to.</param>
        /// <param name="transports">A bitmask comprised of one or more <see cref="HttpTransportType"/> that specify what transports the client should use.</param>
        public HttpConnection(Uri url, HttpTransportType transports)
            : this(url, transports, loggerFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnection"/> class.
        /// </summary>
        /// <param name="url">The URL to connect to.</param>
        /// <param name="transports">A bitmask comprised of one or more <see cref="HttpTransportType"/> that specify what transports the client should use.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public HttpConnection(Uri url, HttpTransportType transports, ILoggerFactory loggerFactory)
            : this(CreateHttpOptions(url, transports), loggerFactory)
        {
        }

        private static HttpConnectionOptions CreateHttpOptions(Uri url, HttpTransportType transports)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
            return new HttpConnectionOptions { Url = url, Transports = transports };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpConnection"/> class.
        /// </summary>
        /// <param name="httpConnectionOptions">The connection options to use.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public HttpConnection(HttpConnectionOptions httpConnectionOptions, ILoggerFactory loggerFactory)
        {
            if (httpConnectionOptions.Url == null)
            {
                throw new ArgumentException("Options does not have a URL specified.", nameof(httpConnectionOptions));
            }

            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

            _logger = _loggerFactory.CreateLogger<HttpConnection>();
            _httpConnectionOptions = httpConnectionOptions;

            if (!httpConnectionOptions.SkipNegotiation || httpConnectionOptions.Transports != HttpTransportType.WebSockets)
            {
                _httpClient = CreateHttpClient();
            }

            _transportFactory = new DefaultTransportFactory(httpConnectionOptions.Transports, _loggerFactory, _httpClient, httpConnectionOptions, GetAccessTokenAsync);
            _logScope = new ConnectionLogScope();

            Features.Set<IConnectionInherentKeepAliveFeature>(this);
        }

        // Used by unit tests
        internal HttpConnection(HttpConnectionOptions httpConnectionOptions, ILoggerFactory loggerFactory, ITransportFactory transportFactory)
            : this(httpConnectionOptions, loggerFactory)
        {
            _transportFactory = transportFactory;
        }

        /// <summary>
        /// Starts the connection.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous start.</returns>
        /// <remarks>
        /// A connection cannot be restarted after it has stopped. To restart a connection
        /// a new instance should be created using the same options.
        /// </remarks>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return StartAsync(TransferFormat.Binary, cancellationToken);
        }

        /// <summary>
        /// Starts the connection using the specified transfer format.
        /// </summary>
        /// <param name="transferFormat">The transfer format the connection should use.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous start.</returns>
        /// <remarks>
        /// A connection cannot be restarted after it has stopped. To restart a connection
        /// a new instance should be created using the same options.
        /// </remarks>
        public async Task StartAsync(TransferFormat transferFormat, CancellationToken cancellationToken = default)
        {
            using (_logger.BeginScope(_logScope))
            {
                await StartAsyncCore(transferFormat).ForceAsync();
            }
        }

        private async Task StartAsyncCore(TransferFormat transferFormat)
        {
            CheckDisposed();

            if (_started)
            {
                Log.SkippingStart(_logger);
                return;
            }

            await _connectionLock.WaitAsync();
            try
            {
                CheckDisposed();

                if (_started)
                {
                    Log.SkippingStart(_logger);
                    return;
                }

                Log.Starting(_logger);

                await SelectAndStartTransport(transferFormat);

                _started = true;
                Log.Started(_logger);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// Disposes the connection.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous dispose.</returns>
        /// <remarks>
        /// A connection cannot be restarted after it has stopped. To restart a connection
        /// a new instance should be created using the same options.
        /// </remarks>
        public async Task DisposeAsync()
        {
            using (_logger.BeginScope(_logScope))
            {
                await DisposeAsyncCore().ForceAsync();
            }
        }

        private async Task DisposeAsyncCore()
        {
            if (_disposed)
            {
                return;
            }

            await _connectionLock.WaitAsync();
            try
            {
                if (!_disposed && _started)
                {
                    Log.DisposingHttpConnection(_logger);

                    // Stop the transport, but we don't care if it throws.
                    // The transport should also have completed the pipe with this exception.
                    try
                    {
                        await _transport.StopAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.TransportThrewExceptionOnStop(_logger, ex);
                    }

                    Log.Disposed(_logger);
                }
                else
                {
                    Log.SkippingDispose(_logger);
                }

                _httpClient?.Dispose();
            }
            finally
            {
                // We want to do these things even if the WaitForWriterToComplete/WaitForReaderToComplete fails
                if (!_disposed)
                {
                    _disposed = true;
                }

                _connectionLock.Release();
            }
        }

        private async Task SelectAndStartTransport(TransferFormat transferFormat)
        {
            var uri = _httpConnectionOptions.Url;
            // Set the initial access token provider back to the original one from options
            _accessTokenProvider = _httpConnectionOptions.AccessTokenProvider;

            var transportExceptions = new List<Exception>();

            if (_httpConnectionOptions.SkipNegotiation)
            {
                if (_httpConnectionOptions.Transports == HttpTransportType.WebSockets)
                {
                    Log.StartingTransport(_logger, _httpConnectionOptions.Transports, uri);
                    await StartTransport(uri, _httpConnectionOptions.Transports, transferFormat);
                }
                else
                {
                    throw new InvalidOperationException("Negotiation can only be skipped when using the WebSocket transport directly.");
                }
            }
            else
            {
                NegotiationResponse negotiationResponse;
                var redirects = 0;

                do
                {
                    negotiationResponse = await GetNegotiationResponseAsync(uri);

                    if (negotiationResponse.Url != null)
                    {
                        uri = new Uri(negotiationResponse.Url);
                    }

                    if (negotiationResponse.AccessToken != null)
                    {
                        string accessToken = negotiationResponse.AccessToken;
                        // Set the current access token factory so that future requests use this access token
                        _accessTokenProvider = () => Task.FromResult(accessToken);
                    }

                    redirects++;
                }
                while (negotiationResponse.Url != null && redirects < _maxRedirects);

                if (redirects == _maxRedirects && negotiationResponse.Url != null)
                {
                    throw new InvalidOperationException("Negotiate redirection limit exceeded.");
                }

                // This should only need to happen once
                var connectUrl = CreateConnectUrl(uri, negotiationResponse.ConnectionId);

                // We're going to search for the transfer format as a string because we don't want to parse
                // all the transfer formats in the negotiation response, and we want to allow transfer formats
                // we don't understand in the negotiate response.
                var transferFormatString = transferFormat.ToString();

                foreach (var transport in negotiationResponse.AvailableTransports)
                {
                    if (!Enum.TryParse<HttpTransportType>(transport.Transport, out var transportType))
                    {
                        Log.TransportNotSupported(_logger, transport.Transport);
                        transportExceptions.Add(new TransportFailedException(transport.Transport, "The transport is not supported by the client."));
                        continue;
                    }

                    if (transportType == HttpTransportType.WebSockets && !IsWebSocketsSupported())
                    {
                        Log.WebSocketsNotSupportedByOperatingSystem(_logger);
                        transportExceptions.Add(new TransportFailedException("WebSockets", "The transport is not supported on this operating system."));
                        continue;
                    }

                    try
                    {
                        if ((transportType & _httpConnectionOptions.Transports) == 0)
                        {
                            Log.TransportDisabledByClient(_logger, transportType);
                            transportExceptions.Add(new TransportFailedException(transportType.ToString(), "The transport is disabled by the client."));
                        }
                        else if (!transport.TransferFormats.Contains(transferFormatString, StringComparer.Ordinal))
                        {
                            Log.TransportDoesNotSupportTransferFormat(_logger, transportType, transferFormat);
                            transportExceptions.Add(new TransportFailedException(transportType.ToString(), $"The transport does not support the '{transferFormat}' transfer format."));
                        }
                        else
                        {
                            // The negotiation response gets cleared in the fallback scenario.
                            if (negotiationResponse == null)
                            {
                                negotiationResponse = await GetNegotiationResponseAsync(uri);
                                connectUrl = CreateConnectUrl(uri, negotiationResponse.ConnectionId);
                            }

                            Log.StartingTransport(_logger, transportType, connectUrl);
                            await StartTransport(connectUrl, transportType, transferFormat);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.TransportFailed(_logger, transportType, ex);

                        transportExceptions.Add(new TransportFailedException(transportType.ToString(), ex.Message, ex));

                        // Try the next transport
                        // Clear the negotiation response so we know to re-negotiate.
                        negotiationResponse = null;
                    }
                }
            }

            if (_transport == null)
            {
                if (transportExceptions.Count > 0)
                {
                    throw new AggregateException("Unable to connect to the server with any of the available transports.", transportExceptions);
                }
                else
                {
                    throw new NoTransportSupportedException("None of the transports supported by the client are supported by the server.");
                }
            }
        }

        private async Task<NegotiationResponse> NegotiateAsync(Uri url, HttpClient httpClient, ILogger logger)
        {
            try
            {
                // Get a connection ID from the server
                Log.EstablishingConnection(logger, url);
                var urlBuilder = new UriBuilder(url);
                if (!urlBuilder.Path.EndsWith("/"))
                {
                    urlBuilder.Path += "/";
                }
                urlBuilder.Path += "negotiate";

                using (var request = new HttpRequestMessage(HttpMethod.Post, urlBuilder.Uri))
                {
                    // Corefx changed the default version and High Sierra curlhandler tries to upgrade request
                    request.Version = new Version(1, 1);

                    // ResponseHeadersRead instructs SendAsync to return once headers are read
                    // rather than buffer the entire response. This gives a small perf boost.
                    // Note that it is important to dispose of the response when doing this to
                    // avoid leaving the connection open.
                    using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        NegotiationResponse negotiateResponse;
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            negotiateResponse = NegotiateProtocol.ParseResponse(responseStream);
                        }
                        if (!string.IsNullOrEmpty(negotiateResponse.Error))
                        {
                            throw new Exception(negotiateResponse.Error);
                        }
                        Log.ConnectionEstablished(_logger, negotiateResponse.ConnectionId);
                        return negotiateResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorWithNegotiation(logger, url, ex);
                throw;
            }
        }

        private static Uri CreateConnectUrl(Uri url, string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new FormatException("Invalid connection id.");
            }

            return Utils.AppendQueryString(url, "id=" + connectionId);
        }

        private async Task StartTransport(Uri connectUrl, HttpTransportType transportType, TransferFormat transferFormat)
        {
            // Construct the transport
            var transport = _transportFactory.CreateTransport(transportType);

            // Start the transport, giving it one end of the pipe
            try
            {
                await transport.StartAsync(connectUrl, transferFormat);
            }
            catch (Exception ex)
            {
                Log.ErrorStartingTransport(_logger, transportType, ex);

                _transport = null;
                throw;
            }

            // Disable keep alives for long polling
            _hasInherentKeepAlive = transportType == HttpTransportType.LongPolling;

            // We successfully started, set the transport properties (we don't want to set these until the transport is definitely running).
            _transport = transport;

            Log.TransportStarted(_logger, transportType);
        }

        private HttpClient CreateHttpClient()
        {
            var httpClientHandler = new HttpClientHandler();
            HttpMessageHandler httpMessageHandler = httpClientHandler;

            if (_httpConnectionOptions != null)
            {
                if (_httpConnectionOptions.Proxy != null)
                {
                    httpClientHandler.Proxy = _httpConnectionOptions.Proxy;
                }
                if (_httpConnectionOptions.Cookies != null)
                {
                    httpClientHandler.CookieContainer = _httpConnectionOptions.Cookies;
                }

                // Only access HttpClientHandler.ClientCertificates if the user has configured client certs
                // Mono does not support client certs and will throw NotImplementedException
                // https://github.com/aspnet/SignalR/issues/2232
                var clientCertificates = _httpConnectionOptions.ClientCertificates;
                if (clientCertificates?.Count > 0)
                {
                    httpClientHandler.ClientCertificates.AddRange(clientCertificates);
                }

                if (_httpConnectionOptions.UseDefaultCredentials != null)
                {
                    httpClientHandler.UseDefaultCredentials = _httpConnectionOptions.UseDefaultCredentials.Value;
                }
                if (_httpConnectionOptions.Credentials != null)
                {
                    httpClientHandler.Credentials = _httpConnectionOptions.Credentials;
                }

                httpMessageHandler = httpClientHandler;
                if (_httpConnectionOptions.HttpMessageHandlerFactory != null)
                {
                    httpMessageHandler = _httpConnectionOptions.HttpMessageHandlerFactory(httpClientHandler);
                    if (httpMessageHandler == null)
                    {
                        throw new InvalidOperationException("Configured HttpMessageHandlerFactory did not return a value.");
                    }
                }

                // Apply the authorization header in a handler instead of a default header because it can change with each request
                httpMessageHandler = new AccessTokenHttpMessageHandler(httpMessageHandler, this);
            }

            // Wrap message handler after HttpMessageHandlerFactory to ensure not overridden
            httpMessageHandler = new LoggingHttpMessageHandler(httpMessageHandler, _loggerFactory);

            var httpClient = new HttpClient(httpMessageHandler);
            httpClient.Timeout = HttpClientTimeout;

            // Start with the user agent header
            httpClient.DefaultRequestHeaders.UserAgent.Add(Constants.UserAgentHeader);

            // Apply any headers configured on the HttpConnectionOptions
            if (_httpConnectionOptions?.Headers != null)
            {
                foreach (var header in _httpConnectionOptions.Headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            httpClient.DefaultRequestHeaders.Remove("X-Requested-With");
            // Tell auth middleware to 401 instead of redirecting
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            return httpClient;
        }

        internal Task<string> GetAccessTokenAsync()
        {
            if (_accessTokenProvider == null)
            {
                return _noAccessToken;
            }
            return _accessTokenProvider();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpConnection));
            }
        }

        private static bool IsWebSocketsSupported()
        {
#if NETCOREAPP3_0
            // .NET Core 2.1 and above has a managed implementation
            return true;
#else
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!isWindows)
            {
                // Assume other OSes have websockets
                return true;
            }
            else
            {
                // Windows 8 and above has websockets
                return Environment.OSVersion.Version >= Windows8Version;
            }
#endif
        }

        private async Task<NegotiationResponse> GetNegotiationResponseAsync(Uri uri)
        {
            var negotiationResponse = await NegotiateAsync(uri, _httpClient, _logger);
            _connectionId = negotiationResponse.ConnectionId;
            _logScope.ConnectionId = _connectionId;
            return negotiationResponse;
        }
    }
}
