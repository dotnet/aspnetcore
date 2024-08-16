// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Connections.Client;

/// <summary>
/// Used to make a connection to an ASP.NET Core ConnectionHandler using an HTTP-based transport.
/// </summary>
public partial class HttpConnection : ConnectionContext, IConnectionInherentKeepAliveFeature
{
    // Not configurable on purpose, high enough that if we reach here, it's likely
    // a buggy server
    private const int _maxRedirects = 100;
    private const int _protocolVersionNumber = 1;
    private static readonly Task<string?> _noAccessToken = Task.FromResult<string?>(null);

    private static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(120);

    internal readonly ILogger _logger;

    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
    private bool _started;
    private bool _disposed;
    private bool _hasInherentKeepAlive;

    private readonly HttpClient? _httpClient;
    private readonly HttpConnectionOptions _httpConnectionOptions;
    private ITransport? _transport;
    private readonly ITransportFactory _transportFactory;
    private string? _connectionId;
    private readonly ConnectionLogScope _logScope;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Uri _url;
    private Func<Task<string?>>? _accessTokenProvider;

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
    public override string? ConnectionId
    {
#pragma warning disable CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
        get => _connectionId;
#pragma warning restore CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
        set => throw new InvalidOperationException("The ConnectionId is set internally and should not be set by user code.");
    }

    /// <inheritdoc />
    public override IDictionary<object, object?> Items { get; set; } = new ConnectionItems();

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
    /// <param name="transports">A bitmask combining one or more <see cref="HttpTransportType"/> values that specify what transports the client should use.</param>
    public HttpConnection(Uri url, HttpTransportType transports)
        : this(url, transports, loggerFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnection"/> class.
    /// </summary>
    /// <param name="url">The URL to connect to.</param>
    /// <param name="transports">A bitmask combining one or more <see cref="HttpTransportType"/> values that specify what transports the client should use.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public HttpConnection(Uri url, HttpTransportType transports, ILoggerFactory? loggerFactory)
        : this(CreateHttpOptions(url, transports), loggerFactory)
    {
    }

    private static HttpConnectionOptions CreateHttpOptions(Uri url, HttpTransportType transports)
    {
        ArgumentNullThrowHelper.ThrowIfNull(url);
        return new HttpConnectionOptions { Url = url, Transports = transports };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnection"/> class.
    /// </summary>
    /// <param name="httpConnectionOptions">The connection options to use.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public HttpConnection(HttpConnectionOptions httpConnectionOptions, ILoggerFactory? loggerFactory)
    {
        ArgumentNullThrowHelper.ThrowIfNull(httpConnectionOptions);

        if (httpConnectionOptions.Url == null)
        {
            throw new ArgumentException("Options does not have a URL specified.", nameof(httpConnectionOptions));
        }

        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        _logger = _loggerFactory.CreateLogger(typeof(HttpConnection));
        _httpConnectionOptions = httpConnectionOptions;

        _url = _httpConnectionOptions.Url;

        if (!httpConnectionOptions.SkipNegotiation || httpConnectionOptions.Transports != HttpTransportType.WebSockets)
        {
            _httpClient = CreateHttpClient();
        }

        if (httpConnectionOptions.Transports == HttpTransportType.ServerSentEvents && OperatingSystem.IsBrowser())
        {
            throw new ArgumentException("ServerSentEvents can not be the only transport specified when running in the browser.", nameof(httpConnectionOptions));
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
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return StartAsync(_httpConnectionOptions.DefaultTransferFormat, cancellationToken);
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
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public async Task StartAsync(TransferFormat transferFormat, CancellationToken cancellationToken = default)
    {
        using (_logger.BeginScope(_logScope))
        {
            await StartAsyncCore(transferFormat, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task StartAsyncCore(TransferFormat transferFormat, CancellationToken cancellationToken)
    {
        CheckDisposed();

        if (_started)
        {
            Log.SkippingStart(_logger);
            return;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CheckDisposed();

            if (_started)
            {
                Log.SkippingStart(_logger);
                return;
            }

            Log.Starting(_logger);

            await SelectAndStartTransport(transferFormat, cancellationToken).ConfigureAwait(false);

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
    public override async ValueTask DisposeAsync()
    {
        using (_logger.BeginScope(_logScope))
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }
    }

    private async Task DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_disposed && _started)
            {
                Log.DisposingHttpConnection(_logger);

                // Stop the transport, but we don't care if it throws.
                // The transport should also have completed the pipe with this exception.
                try
                {
                    await _transport!.StopAsync().ConfigureAwait(false);
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

    private async Task SelectAndStartTransport(TransferFormat transferFormat, CancellationToken cancellationToken)
    {
        var uri = _url;
        // Set the initial access token provider back to the original one from options
        _accessTokenProvider = _httpConnectionOptions.AccessTokenProvider;

        var transportExceptions = new List<Exception>();

        if (_httpConnectionOptions.SkipNegotiation)
        {
            if (_httpConnectionOptions.Transports == HttpTransportType.WebSockets)
            {
                Log.StartingTransport(_logger, _httpConnectionOptions.Transports, uri);
                await StartTransport(uri, _httpConnectionOptions.Transports, transferFormat, cancellationToken, useStatefulReconnect: false).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException("Negotiation can only be skipped when using the WebSocket transport directly.");
            }
        }
        else
        {
            NegotiationResponse? negotiationResponse;
            var redirects = 0;

            do
            {
                negotiationResponse = await GetNegotiationResponseAsync(uri, cancellationToken).ConfigureAwait(false);

                if (negotiationResponse.Url != null)
                {
                    uri = new Uri(negotiationResponse.Url);
                }

                if (negotiationResponse.AccessToken != null)
                {
                    string accessToken = negotiationResponse.AccessToken;
                    // Set the current access token factory so that future requests use this access token
                    _accessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }

                redirects++;
            }
            while (negotiationResponse.Url != null && redirects < _maxRedirects);

            if (redirects == _maxRedirects && negotiationResponse.Url != null)
            {
                throw new InvalidOperationException("Negotiate redirection limit exceeded.");
            }

            // Set the final negotiated URI as the endpoint.
            RemoteEndPoint = new UriEndPoint(Utils.CreateEndPointUri(uri));

            // This should only need to happen once
            var connectUrl = CreateConnectUrl(uri, negotiationResponse.ConnectionToken);

            // We're going to search for the transfer format as a string because we don't want to parse
            // all the transfer formats in the negotiation response, and we want to allow transfer formats
            // we don't understand in the negotiate response.
            var transferFormatString = transferFormat.ToString();

            foreach (var transport in negotiationResponse.AvailableTransports!)
            {
                if (!Enum.TryParse<HttpTransportType>(transport.Transport, out var transportType))
                {
                    Log.TransportNotSupported(_logger, transport.Transport!);
                    transportExceptions.Add(new TransportFailedException(transport.Transport!, "The transport is not supported by the client."));
                    continue;
                }

                if (transportType == HttpTransportType.WebSockets && !IsWebSocketsSupported())
                {
                    Log.WebSocketsNotSupportedByOperatingSystem(_logger);
                    transportExceptions.Add(new TransportFailedException("WebSockets", "The transport is not supported on this operating system."));
                    continue;
                }

                if (transportType == HttpTransportType.ServerSentEvents && OperatingSystem.IsBrowser())
                {
                    Log.ServerSentEventsNotSupportedByBrowser(_logger);
                    transportExceptions.Add(new TransportFailedException("ServerSentEvents", "The transport is not supported in the browser."));
                    continue;
                }

                try
                {
                    if ((transportType & _httpConnectionOptions.Transports) == 0)
                    {
                        Log.TransportDisabledByClient(_logger, transportType);
                        transportExceptions.Add(new TransportFailedException(transportType.ToString(), "The transport is disabled by the client."));
                    }
                    else if (!transport.TransferFormats!.Contains(transferFormatString, StringComparer.Ordinal))
                    {
                        Log.TransportDoesNotSupportTransferFormat(_logger, transportType, transferFormat);
                        transportExceptions.Add(new TransportFailedException(transportType.ToString(), $"The transport does not support the '{transferFormat}' transfer format."));
                    }
                    else
                    {
                        // The negotiation response gets cleared in the fallback scenario.
                        if (negotiationResponse == null)
                        {
                            // Temporary until other transports work
                            _httpConnectionOptions.UseStatefulReconnect = transportType == HttpTransportType.WebSockets ? _httpConnectionOptions.UseStatefulReconnect : false;
                            negotiationResponse = await GetNegotiationResponseAsync(uri, cancellationToken).ConfigureAwait(false);
                            connectUrl = CreateConnectUrl(uri, negotiationResponse.ConnectionToken);

                            // Set the final negotiated URI as the endpoint.
                            RemoteEndPoint = new UriEndPoint(Utils.CreateEndPointUri(uri));
                        }

                        Log.StartingTransport(_logger, transportType, uri);
                        await StartTransport(connectUrl, transportType, transferFormat, cancellationToken, negotiationResponse.UseStatefulReconnect).ConfigureAwait(false);
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

    private async Task<NegotiationResponse> NegotiateAsync(Uri url, HttpClient httpClient, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            // Get a connection ID from the server
            Log.EstablishingConnection(logger, url);
            var urlBuilder = new UriBuilder(url);
            if (!urlBuilder.Path.EndsWith("/", StringComparison.Ordinal))
            {
                urlBuilder.Path += "/";
            }
            urlBuilder.Path += "negotiate";
            Uri uri;
            if (urlBuilder.Query.Contains("negotiateVersion"))
            {
                uri = urlBuilder.Uri;
            }
            else
            {
                uri = Utils.AppendQueryString(urlBuilder.Uri, $"negotiateVersion={_protocolVersionNumber}");
            }

            if (_httpConnectionOptions.UseStatefulReconnect)
            {
                uri = Utils.AppendQueryString(uri, "useStatefulReconnect=true");
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
#if NET5_0_OR_GREATER
                request.Options.Set(new HttpRequestOptionsKey<bool>("IsNegotiate"), true);
#else
                request.Properties.Add("IsNegotiate", true);
#endif
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                // ResponseHeadersRead instructs SendAsync to return once headers are read
                // rather than buffer the entire response. This gives a small perf boost.
                // Note that it is important to dispose of the response when doing this to
                // avoid leaving the connection open.
                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods
                    var responseBuffer = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#pragma warning restore CA2016 // Forward the 'CancellationToken' parameter to methods
                    var negotiateResponse = NegotiateProtocol.ParseResponse(responseBuffer);
                    if (!string.IsNullOrEmpty(negotiateResponse.Error))
                    {
                        throw new InvalidOperationException(negotiateResponse.Error);
                    }
                    Log.ConnectionEstablished(_logger, negotiateResponse.ConnectionId!);
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

    private static Uri CreateConnectUrl(Uri url, string? connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new FormatException("Invalid connection id.");
        }

        return Utils.AppendQueryString(url, $"id={connectionId}");
    }

    private async Task StartTransport(Uri connectUrl, HttpTransportType transportType, TransferFormat transferFormat,
        CancellationToken cancellationToken, bool useStatefulReconnect)
    {
        // Construct the transport
        var transport = _transportFactory.CreateTransport(transportType, useStatefulReconnect);

        // Start the transport, giving it one end of the pipe
        try
        {
            await transport.StartAsync(connectUrl, transferFormat, cancellationToken).ConfigureAwait(false);
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

        if (useStatefulReconnect && _transport is IStatefulReconnectFeature reconnectFeature)
        {
#pragma warning disable CA2252 // This API requires opting into preview features
            Features.Set(reconnectFeature);
#pragma warning restore CA2252 // This API requires opting into preview features
        }

        Log.TransportStarted(_logger, transportType);
    }

    private HttpClient CreateHttpClient()
    {
        var httpClientHandler = new HttpClientHandler();
        HttpMessageHandler httpMessageHandler = httpClientHandler;

        var isBrowser = OperatingSystem.IsBrowser();
        var allowHttp2 = true;

        if (_httpConnectionOptions != null)
        {
            if (!isBrowser)
            {
                // Configure options that do not work in the browser inside this if-block
                if (_httpConnectionOptions.Proxy != null)
                {
                    httpClientHandler.Proxy = _httpConnectionOptions.Proxy;
                }

                try
                {
                    // On supported platforms, we need to pass the cookie container to the http client
                    // so that we can capture any cookies from the negotiate response and give them to WebSockets.
                    httpClientHandler.CookieContainer = _httpConnectionOptions.Cookies;
                }
                // Some variants of Mono do not support client certs or cookies and will throw NotImplementedException or NotSupportedException
                // Also WASM doesn't support some settings in the browser
                catch (Exception ex) when (ex is NotSupportedException || ex is NotImplementedException)
                {
                    Log.CookiesNotSupported(_logger);
                }

                // Only access HttpClientHandler.ClientCertificates
                // if the user has configured those options
                // https://github.com/aspnet/SignalR/issues/2232

                var clientCertificates = _httpConnectionOptions.ClientCertificates;
                if (clientCertificates?.Count > 0)
                {
                    httpClientHandler.ClientCertificates.AddRange(clientCertificates);
                }

                if (_httpConnectionOptions.UseDefaultCredentials != null)
                {
                    httpClientHandler.UseDefaultCredentials = _httpConnectionOptions.UseDefaultCredentials.Value;
                    // Negotiate Auth isn't supported over HTTP/2 and HttpClient does not gracefully fallback to HTTP/1.1 in that case
                    // https://github.com/dotnet/runtime/issues/1582
                    allowHttp2 = !_httpConnectionOptions.UseDefaultCredentials.Value;
                }

                if (_httpConnectionOptions.Credentials != null)
                {
                    httpClientHandler.Credentials = _httpConnectionOptions.Credentials;
                    // Negotiate Auth isn't supported over HTTP/2 and HttpClient does not gracefully fallback to HTTP/1.1 in that case
                    // https://github.com/dotnet/runtime/issues/1582
                    allowHttp2 = false;
                }
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

        if (allowHttp2)
        {
            httpMessageHandler = new Http2HttpMessageHandler(httpMessageHandler);
        }

        var httpClient = new HttpClient(httpMessageHandler);
        httpClient.Timeout = HttpClientTimeout;

        var userSetUserAgent = false;

        // Apply any headers configured on the HttpConnectionOptions
        if (_httpConnectionOptions?.Headers != null)
        {
            foreach (var header in _httpConnectionOptions.Headers)
            {
                // Check if the key is User-Agent and remove if empty string then replace if it exists.
                if (string.Equals(header.Key, Constants.UserAgent, StringComparison.OrdinalIgnoreCase))
                {
                    userSetUserAgent = true;
                    if (string.IsNullOrEmpty(header.Value))
                    {
                        httpClient.DefaultRequestHeaders.Remove(header.Key);
                    }
                    else if (httpClient.DefaultRequestHeaders.Contains(header.Key))
                    {
                        httpClient.DefaultRequestHeaders.Remove(header.Key);
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    else
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                else
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }

        // Apply default user agent only if user hasn't specified one (empty or not)
        // Don't pre-emptively set this, some frameworks (mono) have different user agent format rules,
        // so allowing a user to set an empty one avoids throwing on those frameworks.
        if (!userSetUserAgent)
        {
            httpClient.DefaultRequestHeaders.Add(Constants.UserAgent, Constants.UserAgentHeader);
        }

        httpClient.DefaultRequestHeaders.Remove("X-Requested-With");
        // Tell auth middleware to 401 instead of redirecting
        httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        return httpClient;
    }

    internal Task<string?> GetAccessTokenAsync()
    {
        if (_accessTokenProvider == null)
        {
            return _noAccessToken;
        }
        return _accessTokenProvider();
    }

    private void CheckDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }

    private static bool IsWebSocketsSupported()
    {
#if NETSTANDARD2_1 || NETCOREAPP
        // .NET Core 2.1 and above has a managed implementation
        return true;
#else
        try
        {
            new ClientWebSocket().Dispose();
            return true;
        }
        catch
        {
            return false;
        }
#endif
    }

    private async Task<NegotiationResponse> GetNegotiationResponseAsync(Uri uri, CancellationToken cancellationToken)
    {
        var negotiationResponse = await NegotiateAsync(uri, _httpClient!, _logger, cancellationToken).ConfigureAwait(false);
        // If the negotiationVersion is greater than zero then we know that the negotiation response contains a
        // connectionToken that will be required to conenct. Otherwise we just set the connectionId and the
        // connectionToken on the client to the same value.
        _connectionId = negotiationResponse.ConnectionId!;
        if (negotiationResponse.Version == 0)
        {
            negotiationResponse.ConnectionToken = _connectionId;
        }

        _logScope.ConnectionId = _connectionId;
        return negotiationResponse;
    }
}
