// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Net;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class RequestContext :
    IHttpRequestFeature,
    IHttpRequestBodyDetectionFeature,
    IHttpConnectionFeature,
    IHttpResponseFeature,
    IHttpResponseBodyFeature,
    ITlsConnectionFeature,
    ITlsHandshakeFeature,
    // ITlsTokenBindingFeature, TODO: https://github.com/aspnet/HttpSysServer/issues/231
    IHttpRequestLifetimeFeature,
    IHttpAuthenticationFeature,
    IHttpUpgradeFeature,
    IHttpRequestIdentifierFeature,
    IHttpMaxRequestBodySizeFeature,
    IHttpBodyControlFeature,
    IHttpSysRequestInfoFeature,
    IHttpSysRequestTimingFeature,
    IHttpResponseTrailersFeature,
    IHttpResetFeature,
    IHttpSysRequestDelegationFeature,
    IConnectionLifetimeNotificationFeature
{
    private IFeatureCollection? _features;
    private bool _enableResponseCaching;

    private Stream? _requestBody;
    private IHeaderDictionary _requestHeaders = default!;
    private string _scheme = default!;
    private string _httpMethod = default!;
    private string? _httpProtocolVersion;
    private string _query = default!;
    private string _pathBase = default!;
    private string _path = default!;
    private string _rawTarget = default!;
    private IPAddress? _remoteIpAddress;
    private IPAddress? _localIpAddress;
    private int _remotePort;
    private int _localPort;
    private string? _connectionId;
    private string? _traceIdentitfier;
    private X509Certificate2? _clientCert;
    private Task<X509Certificate2?>? _clientCertTask;
    private ClaimsPrincipal? _user;
    private Stream _responseStream = default!;
    private PipeWriter? _pipeWriter;
    private bool _bodyCompleted;
    private IHeaderDictionary _responseHeaders = default!;
    private IHeaderDictionary? _responseTrailers;

    private Fields _initializedFields;

    private List<Tuple<Func<object, Task>, object>>? _onStartingActions = new List<Tuple<Func<object, Task>, object>>();
    private List<Tuple<Func<object, Task>, object>>? _onCompletedActions = new List<Tuple<Func<object, Task>, object>>();
    private bool _responseStarted;
    private bool _completed;

    internal IFeatureCollection Features
    {
        get
        {
            Debug.Assert(_features != null);
            return _features;
        }
    }

    [Flags]
    // Fields that may be lazy-initialized
    private enum Fields
    {
        None = 0x0,
        Protocol = 0x1,
        RequestBody = 0x2,
        RequestAborted = 0x4,
        LocalIpAddress = 0x8,
        RemoteIpAddress = 0x10,
        LocalPort = 0x20,
        RemotePort = 0x40,
        ConnectionId = 0x80,
        ClientCertificate = 0x100,
        TraceIdentifier = 0x200,
    }

    protected internal void InitializeFeatures()
    {
        _initialized = true;

        Request = new Request(this);
        Response = new Response(this);

        _features = new FeatureCollection(new StandardFeatureCollection(this));
        _enableResponseCaching = Server.Options.EnableResponseCaching;

        // Pre-initialize any fields that are not lazy at the lower level.
        _requestHeaders = Request.Headers;
        _httpMethod = Request.Method;
        _path = Request.Path;
        _pathBase = Request.PathBase;
        _query = Request.QueryString;
        _rawTarget = Request.RawUrl;
        _scheme = Request.Scheme;

        if (Server.Options.Authentication.AutomaticAuthentication)
        {
            _user = User;
        }

        _responseStream = new ResponseStream(Response.Body, OnResponseStart);
        _responseHeaders = Response.Headers;
    }

    private bool IsNotInitialized(Fields field)
    {
        return (_initializedFields & field) != field;
    }

    private void SetInitialized(Fields field)
    {
        _initializedFields |= field;
    }

    Stream IHttpRequestFeature.Body
    {
        get
        {
            if (IsNotInitialized(Fields.RequestBody))
            {
                _requestBody = Request.Body;
                SetInitialized(Fields.RequestBody);
            }
            return _requestBody!;
        }
        set
        {
            _requestBody = value;
            SetInitialized(Fields.RequestBody);
        }
    }

    IHeaderDictionary IHttpRequestFeature.Headers
    {
        get { return _requestHeaders; }
        set { _requestHeaders = value; }
    }

    string IHttpRequestFeature.Method
    {
        get { return _httpMethod; }
        set { _httpMethod = value; }
    }

    string IHttpRequestFeature.Path
    {
        get { return _path; }
        set { _path = value; }
    }

    string IHttpRequestFeature.PathBase
    {
        get { return _pathBase; }
        set { _pathBase = value; }
    }

    string IHttpRequestFeature.Protocol
    {
        get
        {
            if (IsNotInitialized(Fields.Protocol))
            {
                _httpProtocolVersion = HttpProtocol.GetHttpProtocol(Request.ProtocolVersion);
                SetInitialized(Fields.Protocol);
            }
            return _httpProtocolVersion!;
        }
        set
        {
            _httpProtocolVersion = value;
            SetInitialized(Fields.Protocol);
        }
    }

    string IHttpRequestFeature.QueryString
    {
        get { return _query; }
        set { _query = value; }
    }

    string IHttpRequestFeature.RawTarget
    {
        get { return _rawTarget; }
        set { _rawTarget = value; }
    }

    string IHttpRequestFeature.Scheme
    {
        get { return _scheme; }
        set { _scheme = value; }
    }

    bool IHttpRequestBodyDetectionFeature.CanHaveBody => Request.HasEntityBody;

    IPAddress? IHttpConnectionFeature.LocalIpAddress
    {
        get
        {
            if (IsNotInitialized(Fields.LocalIpAddress))
            {
                _localIpAddress = Request.LocalIpAddress;
                SetInitialized(Fields.LocalIpAddress);
            }
            return _localIpAddress;
        }
        set
        {
            _localIpAddress = value;
            SetInitialized(Fields.LocalIpAddress);
        }
    }

    IPAddress? IHttpConnectionFeature.RemoteIpAddress
    {
        get
        {
            if (IsNotInitialized(Fields.RemoteIpAddress))
            {
                _remoteIpAddress = Request.RemoteIpAddress;
                SetInitialized(Fields.RemoteIpAddress);
            }
            return _remoteIpAddress;
        }
        set
        {
            _remoteIpAddress = value;
            SetInitialized(Fields.RemoteIpAddress);
        }
    }

    int IHttpConnectionFeature.LocalPort
    {
        get
        {
            if (IsNotInitialized(Fields.LocalPort))
            {
                _localPort = Request.LocalPort;
                SetInitialized(Fields.LocalPort);
            }
            return _localPort;
        }
        set
        {
            _localPort = value;
            SetInitialized(Fields.LocalPort);
        }
    }

    int IHttpConnectionFeature.RemotePort
    {
        get
        {
            if (IsNotInitialized(Fields.RemotePort))
            {
                _remotePort = Request.RemotePort;
                SetInitialized(Fields.RemotePort);
            }
            return _remotePort;
        }
        set
        {
            _remotePort = value;
            SetInitialized(Fields.RemotePort);
        }
    }

    string IHttpConnectionFeature.ConnectionId
    {
        get
        {
            if (IsNotInitialized(Fields.ConnectionId))
            {
                _connectionId = Request.ConnectionId.ToString(CultureInfo.InvariantCulture);
                SetInitialized(Fields.ConnectionId);
            }
            return _connectionId!;
        }
        set
        {
            _connectionId = value;
            SetInitialized(Fields.ConnectionId);
        }
    }

    X509Certificate2? ITlsConnectionFeature.ClientCertificate
    {
        get
        {
            if (IsNotInitialized(Fields.ClientCertificate))
            {
                var method = Server.Options.ClientCertificateMethod;
                if (method != ClientCertificateMethod.NoCertificate)
                {
                    _clientCert = Request.ClientCertificate;
                }

                SetInitialized(Fields.ClientCertificate);
            }
            return _clientCert;
        }
        set
        {
            _clientCert = value;
            _clientCertTask = Task.FromResult(value);
            SetInitialized(Fields.ClientCertificate);
        }
    }

    Task<X509Certificate2?> ITlsConnectionFeature.GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        if (_clientCertTask != null)
        {
            return _clientCertTask;
        }

        var tlsFeature = (ITlsConnectionFeature)this;
        var clientCert = tlsFeature.ClientCertificate; // Lazy initialized
        if (clientCert != null
            || Server.Options.ClientCertificateMethod != ClientCertificateMethod.AllowRenegotation
            // Delayed client cert negotiation is not allowed on HTTP/2.
            || Request.ProtocolVersion >= HttpVersion.Version20)
        {
            return _clientCertTask = Task.FromResult(clientCert);
        }

        return _clientCertTask = GetCertificateAsync(cancellationToken);

        async Task<X509Certificate2?> GetCertificateAsync(CancellationToken cancellation)
        {
            return _clientCert = await Request.GetClientCertificateAsync(cancellation);
        }
    }

    internal ITlsConnectionFeature? GetTlsConnectionFeature()
    {
        return Request.IsHttps ? this : null;
    }

    internal ITlsHandshakeFeature? GetTlsHandshakeFeature()
    {
        return Request.IsHttps ? this : null;
    }

    internal IHttpResponseTrailersFeature? GetResponseTrailersFeature()
    {
        if (Request.ProtocolVersion >= HttpVersion.Version20 && HttpApi.SupportsTrailers)
        {
            return this;
        }
        return null;
    }

    internal IHttpResetFeature? GetResetFeature()
    {
        if (Request.ProtocolVersion >= HttpVersion.Version20 && HttpApi.SupportsReset)
        {
            return this;
        }
        return null;
    }

    internal IConnectionLifetimeNotificationFeature? GetConnectionLifetimeNotificationFeature()
    {
        return this;
    }

    /* TODO: https://github.com/aspnet/HttpSysServer/issues/231
    byte[] ITlsTokenBindingFeature.GetProvidedTokenBindingId() => Request.GetProvidedTokenBindingId();

    byte[] ITlsTokenBindingFeature.GetReferredTokenBindingId() => Request.GetReferredTokenBindingId();

    internal ITlsTokenBindingFeature GetTlsTokenBindingFeature()
    {
        return Request.IsHttps ? this : null;
    }
    */

    void IHttpResponseBodyFeature.DisableBuffering()
    {
        // TODO: What about native buffering?
    }

    Stream IHttpResponseFeature.Body
    {
        get { return _responseStream; }
        set { _responseStream = value; }
    }

    Stream IHttpResponseBodyFeature.Stream => _responseStream;

    PipeWriter IHttpResponseBodyFeature.Writer
    {
        get
        {
            if (_pipeWriter == null)
            {
                _pipeWriter = PipeWriter.Create(_responseStream, new StreamPipeWriterOptions(leaveOpen: true));
            }

            return _pipeWriter;
        }
    }

    IHeaderDictionary IHttpResponseFeature.Headers
    {
        get { return _responseHeaders; }
        set { _responseHeaders = value; }
    }

    bool IHttpResponseFeature.HasStarted => _responseStarted;

    void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (_onStartingActions == null)
        {
            throw new InvalidOperationException("Cannot register new callbacks, the response has already started.");
        }

        _onStartingActions.Add(new Tuple<Func<object, Task>, object>(callback, state));
    }

    void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (_onCompletedActions == null)
        {
            throw new InvalidOperationException("Cannot register new callbacks, the response has already completed.");
        }

        _onCompletedActions.Add(new Tuple<Func<object, Task>, object>(callback, state));
    }

    string? IHttpResponseFeature.ReasonPhrase
    {
        get { return Response.ReasonPhrase; }
        set { Response.ReasonPhrase = value; }
    }

    int IHttpResponseFeature.StatusCode
    {
        get { return Response.StatusCode; }
        set { Response.StatusCode = value; }
    }

    async Task IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
    {
        await OnResponseStart();
        await Response.SendFileAsync(path, offset, length, cancellation);
    }

    Task IHttpResponseBodyFeature.StartAsync(CancellationToken cancellation)
    {
        return OnResponseStart();
    }

    Task IHttpResponseBodyFeature.CompleteAsync() => CompleteAsync();

    void IHttpResetFeature.Reset(int errorCode)
    {
        SetResetCode(errorCode);
        Abort();
    }

    internal async Task CompleteAsync()
    {
        if (!_responseStarted)
        {
            await OnResponseStart();
        }

        if (!_bodyCompleted)
        {
            _bodyCompleted = true;
            if (_pipeWriter != null)
            {
                // Flush and complete the pipe
                await _pipeWriter.CompleteAsync();
            }

            // Ends the response body.
            Response.Dispose();
        }
    }

    CancellationToken IHttpRequestLifetimeFeature.RequestAborted
    {
        get
        {
            if (IsNotInitialized(Fields.RequestAborted))
            {
                _disconnectToken = DisconnectToken;
                SetInitialized(Fields.RequestAborted);
            }
            return _disconnectToken!.Value;
        }
        set
        {
            _disconnectToken = value;
            SetInitialized(Fields.RequestAborted);
        }
    }

    void IHttpRequestLifetimeFeature.Abort() => Abort();

    bool IHttpUpgradeFeature.IsUpgradableRequest => IsUpgradableRequest;

    async Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
    {
        await OnResponseStart();
        return await UpgradeAsync();
    }

    ClaimsPrincipal? IHttpAuthenticationFeature.User
    {
        get { return _user; }
        set { _user = value; }
    }

    string IHttpRequestIdentifierFeature.TraceIdentifier
    {
        get
        {
            if (IsNotInitialized(Fields.TraceIdentifier))
            {
                _traceIdentitfier = TraceIdentifier.ToString();
                SetInitialized(Fields.TraceIdentifier);
            }
            return _traceIdentitfier!;
        }
        set
        {
            _traceIdentitfier = value;
            SetInitialized(Fields.TraceIdentifier);
        }
    }

    bool IHttpBodyControlFeature.AllowSynchronousIO
    {
        get => AllowSynchronousIO;
        set => AllowSynchronousIO = value;
    }

    bool IHttpMaxRequestBodySizeFeature.IsReadOnly => Request.HasRequestBodyStarted;

    long? IHttpMaxRequestBodySizeFeature.MaxRequestBodySize
    {
        get => Request.MaxRequestBodySize;
        set => Request.MaxRequestBodySize = value;
    }

    SslProtocols ITlsHandshakeFeature.Protocol => Request.Protocol;

    CipherAlgorithmType ITlsHandshakeFeature.CipherAlgorithm => Request.CipherAlgorithm;

    int ITlsHandshakeFeature.CipherStrength => Request.CipherStrength;

    HashAlgorithmType ITlsHandshakeFeature.HashAlgorithm => Request.HashAlgorithm;

    int ITlsHandshakeFeature.HashStrength => Request.HashStrength;

    ExchangeAlgorithmType ITlsHandshakeFeature.KeyExchangeAlgorithm => Request.KeyExchangeAlgorithm;

    int ITlsHandshakeFeature.KeyExchangeStrength => Request.KeyExchangeStrength;

    string ITlsHandshakeFeature.HostName => Request.SniHostName;

    IHeaderDictionary IHttpResponseTrailersFeature.Trailers
    {
        get => _responseTrailers ??= Response.Trailers;
        set => _responseTrailers = value;
    }

    public bool CanDelegate => Request.CanDelegate;

    CancellationToken IConnectionLifetimeNotificationFeature.ConnectionClosedRequested { get; set; }

    internal async Task OnResponseStart()
    {
        if (_responseStarted)
        {
            return;
        }
        _responseStarted = true;
        await NotifiyOnStartingAsync();
        ConsiderEnablingResponseCache();

        Response.Headers.IsReadOnly = true; // Prohibit further modifications.
    }

    private async Task NotifiyOnStartingAsync()
    {
        var actions = _onStartingActions;
        _onStartingActions = null;
        if (actions == null)
        {
            return;
        }

        // Execute last to first. This mimics a stack unwind.
        for (var i = actions.Count - 1; i >= 0; i--)
        {
            var actionPair = actions[i];
            await actionPair.Item1(actionPair.Item2);
        }
    }

    private void ConsiderEnablingResponseCache()
    {
        if (_enableResponseCaching)
        {
            // We don't have to worry too much about what Http.Sys supports, caching is a best-effort feature.
            // If there's something about the request or response that prevents it from caching then the response
            // will complete normally without caching.
            Response.CacheTtl = GetCacheTtl();
        }
    }

    private TimeSpan? GetCacheTtl()
    {
        var response = Response;
        // A 304 response is supposed to have the same headers as its associated 200 response, including Cache-Control, but the 304 response itself
        // should not be cached. Otherwise Http.Sys will serve the 304 response to all requests without checking conditional headers like If-None-Match.
        if (response.StatusCode == StatusCodes.Status304NotModified)
        {
            return null;
        }

        // Only consider kernel-mode caching if the Cache-Control response header is present.
        var cacheControlHeader = response.Headers[HeaderNames.CacheControl];
        if (string.IsNullOrEmpty(cacheControlHeader))
        {
            return null;
        }

        // Before we check the header value, check for the existence of other headers which would
        // make us *not* want to cache the response.
        if (response.Headers.ContainsKey(HeaderNames.SetCookie)
            || response.Headers.ContainsKey(HeaderNames.Vary)
            || response.Headers.ContainsKey(HeaderNames.Pragma))
        {
            return null;
        }

        // We require 'public' and 's-max-age' or 'max-age' or the Expires header.
        CacheControlHeaderValue? cacheControl;
        if (CacheControlHeaderValue.TryParse(cacheControlHeader.ToString(), out cacheControl) && cacheControl.Public)
        {
            if (cacheControl.SharedMaxAge.HasValue)
            {
                return cacheControl.SharedMaxAge;
            }
            else if (cacheControl.MaxAge.HasValue)
            {
                return cacheControl.MaxAge;
            }

            DateTimeOffset expirationDate;
            if (HeaderUtilities.TryParseDate(response.Headers[HeaderNames.Expires].ToString(), out expirationDate))
            {
                var expiresOffset = expirationDate - DateTimeOffset.UtcNow;
                if (expiresOffset > TimeSpan.Zero)
                {
                    return expiresOffset;
                }
            }
        }

        return null;
    }

    internal Task OnCompleted()
    {
        if (_completed)
        {
            return Task.CompletedTask;
        }
        _completed = true;
        return NotifyOnCompletedAsync();
    }

    private async Task NotifyOnCompletedAsync()
    {
        var actions = _onCompletedActions;
        _onCompletedActions = null;
        if (actions == null)
        {
            return;
        }

        // Execute last to first. This mimics a stack unwind.
        for (var i = actions.Count - 1; i >= 0; i--)
        {
            var actionPair = actions[i];
            await actionPair.Item1(actionPair.Item2);
        }
    }

    public void DelegateRequest(DelegationRule destination)
    {
        Delegate(destination);
        _responseStarted = true;
    }

    void IConnectionLifetimeNotificationFeature.RequestClose()
    {
        // Set the connection close feature if the response hasn't sent headers as yet
        if (!Response.HasStarted)
        {
            Response.Headers[HeaderNames.Connection] = "close";
        }
    }
}
