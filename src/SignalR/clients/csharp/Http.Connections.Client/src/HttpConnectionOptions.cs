// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Http.Connections.Client;

/// <summary>
/// Options used to configure a <see cref="HttpConnection"/> instance.
/// </summary>
public class HttpConnectionOptions
{
    private IDictionary<string, string> _headers;
    private X509CertificateCollection? _clientCertificates;
    private CookieContainer _cookies;
    private ICredentials? _credentials;
    private IWebProxy? _proxy;
    private bool? _useDefaultCredentials;
    private Action<ClientWebSocketOptions>? _webSocketConfiguration;
    private PipeOptions? _transportPipeOptions;
    private PipeOptions? _appPipeOptions;
    private long _transportMaxBufferSize;
    private long _applicationMaxBufferSize;

    // Selected because of the number of client connections is usually much lower than
    // server connections and therefore willing to use more memory. We'll default
    // to a maximum of 1MB buffer;
    private const int DefaultBufferSize = 1 * 1024 * 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionOptions"/> class.
    /// </summary>
    public HttpConnectionOptions()
    {
        _headers = new Dictionary<string, string>();

        // System.Security.Cryptography isn't supported on WASM currently
        if (!OperatingSystem.IsBrowser())
        {
            _clientCertificates = new X509CertificateCollection();
        }

        _cookies = new CookieContainer();

        Transports = HttpTransports.All;
        TransportMaxBufferSize = DefaultBufferSize;
        ApplicationMaxBufferSize = DefaultBufferSize;
    }

    /// <summary>
    /// Gets or sets a delegate for wrapping or replacing the <see cref="HttpMessageHandlerFactory"/>
    /// that will make HTTP requests.
    /// </summary>
    public Func<HttpMessageHandler, HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }

    /// <summary>
    /// Gets or sets a delegate for wrapping or replacing the <see cref="WebSocket"/>
    /// that will be used for the WebSocket transport.
    /// </summary>
    public Func<WebSocketConnectionContext, CancellationToken, ValueTask<WebSocket>>? WebSocketFactory { get; set; }

    /// <summary>
    /// Gets or sets a collection of headers that will be sent with HTTP requests.
    /// </summary>
    public IDictionary<string, string> Headers
    {
        get => _headers;
        set => _headers = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the maximum buffer size for data read by the application before backpressure is applied.
    /// </summary>
    /// <remarks>
    /// The default value is 1MB.
    /// </remarks>
    /// <value>
    /// The default value is 1MB.
    /// </value>
    public long TransportMaxBufferSize
    {
        get => _transportMaxBufferSize;
        set
        {
            ArgumentOutOfRangeThrowHelper.ThrowIfNegative(value);

            _transportMaxBufferSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum buffer size for data written by the application before backpressure is applied.
    /// </summary>
    /// <remarks>
    /// The default value is 1MB.
    /// </remarks>
    /// <value>
    /// The default value is 1MB.
    /// </value>
    public long ApplicationMaxBufferSize
    {
        get => _applicationMaxBufferSize;
        set
        {
            ArgumentOutOfRangeThrowHelper.ThrowIfNegative(value);

            _applicationMaxBufferSize = value;
        }
    }

    // We initialize these lazily based on the state of the options specified here.
    // Though these are mutable it's extremely rare that they would be mutated past the
    // call to initialize the routerware.
    internal PipeOptions TransportPipeOptions => _transportPipeOptions ??= new PipeOptions(pauseWriterThreshold: TransportMaxBufferSize, resumeWriterThreshold: TransportMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);

    internal PipeOptions AppPipeOptions => _appPipeOptions ??= new PipeOptions(pauseWriterThreshold: ApplicationMaxBufferSize, resumeWriterThreshold: ApplicationMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);

    /// <summary>
    /// Gets or sets a collection of client certificates that will be sent with HTTP requests.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public X509CertificateCollection? ClientCertificates
    {
        get
        {
            ThrowIfUnsupportedPlatform();
            return _clientCertificates;
        }
        set
        {
            ThrowIfUnsupportedPlatform();
            _clientCertificates = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Gets or sets a collection of cookies that will be sent with HTTP requests.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public CookieContainer Cookies
    {
        get
        {
            ThrowIfUnsupportedPlatform();
            return _cookies;
        }
        set
        {
            ThrowIfUnsupportedPlatform();
            _cookies = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Gets or sets the URL used to send HTTP requests.
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets or sets a bitmask combining one or more <see cref="HttpTransportType"/> values that specify what transports the client should use to send HTTP requests.
    /// </summary>
    public HttpTransportType Transports { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether negotiation is skipped when connecting to the server.
    /// </summary>
    /// <remarks>
    /// Negotiation can only be skipped when using the <see cref="HttpTransportType.WebSockets"/> transport.
    /// </remarks>
    public bool SkipNegotiation { get; set; }

    /// <summary>
    /// Gets or sets an access token provider that will be called to return a token for each HTTP request.
    /// </summary>
    public Func<Task<string?>>? AccessTokenProvider { get; set; }

    /// <summary>
    /// Gets or sets a close timeout.
    /// </summary>
    public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the credentials used when making HTTP requests.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public ICredentials? Credentials
    {
        get
        {
            ThrowIfUnsupportedPlatform();
            return _credentials;
        }
        set
        {
            ThrowIfUnsupportedPlatform();
            _credentials = value;
        }
    }

    /// <summary>
    /// Gets or sets the proxy used when making HTTP requests.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public IWebProxy? Proxy
    {
        get
        {
            ThrowIfUnsupportedPlatform();
            return _proxy;
        }
        set
        {
            ThrowIfUnsupportedPlatform();
            _proxy = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether default credentials are used when making HTTP requests.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public bool? UseDefaultCredentials
    {
        get
        {
            ThrowIfUnsupportedPlatform();
            return _useDefaultCredentials;
        }
        set
        {
            ThrowIfUnsupportedPlatform();
            _useDefaultCredentials = value;
        }
    }

    /// <summary>
    /// Gets or sets the default <see cref="TransferFormat" /> to use if <see cref="HttpConnection.StartAsync(CancellationToken)"/>
    /// is called instead of <see cref="HttpConnection.StartAsync(TransferFormat, CancellationToken)"/>.
    /// </summary>
    public TransferFormat DefaultTransferFormat { get; set; } = TransferFormat.Binary;

    /// <summary>
    /// Gets or sets a delegate that will be invoked with the <see cref="ClientWebSocketOptions"/> object used
    /// to configure the WebSocket when using the WebSockets transport.
    /// </summary>
    /// <remarks>
    /// This delegate is invoked after headers from <see cref="Headers"/> and the access token from <see cref="AccessTokenProvider"/>
    /// has been applied.
    /// <para />
    /// If <c>ClientWebSocketOptions.HttpVersion</c> is set to <c>2.0</c> or higher, some options like <see cref="ClientWebSocketOptions.Cookies"/> will not be applied. Instead use <see cref="Cookies"/> or the corresponding option on <see cref="HttpConnectionOptions"/>.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    public Action<ClientWebSocketOptions>? WebSocketConfiguration
    {
        get
        {
            ThrowIfUnsupportedPlatform();
            return _webSocketConfiguration;
        }
        set
        {
            ThrowIfUnsupportedPlatform();
            _webSocketConfiguration = value;
        }
    }

    /// <summary>
    /// Setting to enable Stateful Reconnect between client and server, this allows reconnecting that preserves messages sent while disconnected.
    /// Also preserves the <see cref="HttpConnection.ConnectionId"/> when the reconnect is successful.
    /// </summary>
    /// <remarks>
    /// Only works with WebSockets transport currently.
    /// </remarks>
    public bool UseStatefulReconnect { get; set; }

    private static void ThrowIfUnsupportedPlatform()
    {
        if (OperatingSystem.IsBrowser())
        {
            throw new PlatformNotSupportedException();
        }
    }
}
