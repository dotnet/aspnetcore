// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

internal abstract partial class HttpProtocol : IHttpResponseControl
{
    private static readonly byte[] _bytesConnectionClose = Encoding.ASCII.GetBytes("\r\nConnection: close");
    private static readonly byte[] _bytesConnectionKeepAlive = Encoding.ASCII.GetBytes("\r\nConnection: keep-alive");
    private static readonly byte[] _bytesTransferEncodingChunked = Encoding.ASCII.GetBytes("\r\nTransfer-Encoding: chunked");
    private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: " + Constants.ServerName);
    internal const string SchemeHttp = "http";
    internal const string SchemeHttps = "https";

    protected BodyControl? _bodyControl;
    private Stack<KeyValuePair<Func<object, Task>, object>>? _onStarting;
    private Stack<KeyValuePair<Func<object, Task>, object>>? _onCompleted;

    private readonly object _abortLock = new object();
    protected volatile bool _connectionAborted;
    private bool _preventRequestAbortedCancellation;
    private CancellationTokenSource? _abortedCts;
    private CancellationToken? _manuallySetRequestAbortToken;

    protected RequestProcessingStatus _requestProcessingStatus;

    // Keep-alive is default for HTTP/1.1 and HTTP/2; parsing and errors will change its value
    // volatile, see: https://msdn.microsoft.com/en-us/library/x13ttww7.aspx
    protected volatile bool _keepAlive = true;
    // _canWriteResponseBody is set in CreateResponseHeaders.
    // If we are writing with GetMemory/Advance before calling StartAsync, assume we can write and throw away contents if we can't.
    private bool _canWriteResponseBody = true;
    private bool _hasAdvanced;
    private bool _isLeasedMemoryInvalid = true;
    private bool _autoChunk;
    protected Exception? _applicationException;
    private BadHttpRequestException? _requestRejectedException;

    protected HttpVersion _httpVersion;
    // This should only be used by the application, not the server. This is settable on HttpRequest but we don't want that to affect
    // how Kestrel processes requests/responses.
    private string? _httpProtocol;

    private string? _requestId;
    private int _requestHeadersParsed;
    // See MaxRequestHeaderCount, enforced during parsing and may be more relaxed to avoid connection faults.
    protected int _eagerRequestHeadersParsedLimit;

    private long _responseBytesWritten;

    private HttpConnectionContext _context = default!;
    private RouteValueDictionary? _routeValues;
    private Endpoint? _endpoint;

    protected string? _methodText;
    private string? _scheme;
    private Stream? _requestStreamInternal;
    private Stream? _responseStreamInternal;

    public void Initialize(HttpConnectionContext context)
    {
        _context = context;

        ServerOptions = ServiceContext.ServerOptions;

        Reset();

        HttpResponseControl = this;
    }

    public IHttpResponseControl HttpResponseControl { get; set; } = default!;

    public ServiceContext ServiceContext => _context.ServiceContext;
    private IPEndPoint? LocalEndPoint => _context.LocalEndPoint;
    private IPEndPoint? RemoteEndPoint => _context.RemoteEndPoint;
    public ITimeoutControl TimeoutControl => _context.TimeoutControl;

    public IFeatureCollection ConnectionFeatures => _context.ConnectionFeatures;
    public IHttpOutputProducer Output { get; protected set; } = default!;

    protected KestrelTrace Log => ServiceContext.Log;
    private DateHeaderValueManager DateHeaderValueManager => ServiceContext.DateHeaderValueManager;
    // Hold direct reference to ServerOptions since this is used very often in the request processing path
    protected KestrelServerOptions ServerOptions { get; set; } = default!;
    protected string ConnectionId => _context.ConnectionId;

    public string ConnectionIdFeature { get; set; } = default!;
    public bool HasStartedConsumingRequestBody { get; set; }
    public long? MaxRequestBodySize { get; set; }
    public MinDataRate? MinRequestBodyDataRate { get; set; }
    public bool AllowSynchronousIO { get; set; }
    protected int RequestHeadersParsed => _requestHeadersParsed;

    /// <summary>
    /// The request id. <seealso cref="HttpContext.TraceIdentifier"/>
    /// </summary>
    public string TraceIdentifier
    {
        set => _requestId = value;
        get
        {
            // don't generate an ID until it is requested
            if (_requestId == null)
            {
                _requestId = CreateRequestId();
            }
            return _requestId;
        }
    }

    public bool IsUpgradableRequest { get; private set; }
    public bool IsUpgraded { get; set; }
    public bool IsExtendedConnectRequest { get; set; }
    public bool IsExtendedConnectAccepted { get; set; }
    public IPAddress? RemoteIpAddress { get; set; }
    public int RemotePort { get; set; }
    public IPAddress? LocalIpAddress { get; set; }
    public int LocalPort { get; set; }
    // https://datatracker.ietf.org/doc/html/rfc8441 ":protocol"
    public string? ConnectProtocol { get; set; }
    public string? Scheme { get; set; }
    public HttpMethod Method { get; set; }
    public string MethodText => ((IHttpRequestFeature)this).Method;
    public string? PathBase { get; set; }

    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public string? RawTarget { get; set; }

    public string HttpVersion
    {
        get
        {
            if (_httpVersion == Http.HttpVersion.Http3)
            {
                return AspNetCore.Http.HttpProtocol.Http3;
            }
            if (_httpVersion == Http.HttpVersion.Http2)
            {
                return AspNetCore.Http.HttpProtocol.Http2;
            }
            if (_httpVersion == Http.HttpVersion.Http11)
            {
                return AspNetCore.Http.HttpProtocol.Http11;
            }
            if (_httpVersion == Http.HttpVersion.Http10)
            {
                return AspNetCore.Http.HttpProtocol.Http10;
            }

            return string.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            // GetKnownVersion returns versions which ReferenceEquals interned string
            // As most common path, check for this only in fast-path and inline
            if (ReferenceEquals(value, AspNetCore.Http.HttpProtocol.Http3))
            {
                _httpVersion = Http.HttpVersion.Http3;
            }
            else if (ReferenceEquals(value, AspNetCore.Http.HttpProtocol.Http2))
            {
                _httpVersion = Http.HttpVersion.Http2;
            }
            else if (ReferenceEquals(value, AspNetCore.Http.HttpProtocol.Http11))
            {
                _httpVersion = Http.HttpVersion.Http11;
            }
            else if (ReferenceEquals(value, AspNetCore.Http.HttpProtocol.Http10))
            {
                _httpVersion = Http.HttpVersion.Http10;
            }
            else
            {
                HttpVersionSetSlow(value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HttpVersionSetSlow(string value)
    {
        if (AspNetCore.Http.HttpProtocol.IsHttp3(value))
        {
            _httpVersion = Http.HttpVersion.Http3;
        }
        else if (AspNetCore.Http.HttpProtocol.IsHttp2(value))
        {
            _httpVersion = Http.HttpVersion.Http2;
        }
        else if (AspNetCore.Http.HttpProtocol.IsHttp11(value))
        {
            _httpVersion = Http.HttpVersion.Http11;
        }
        else if (AspNetCore.Http.HttpProtocol.IsHttp10(value))
        {
            _httpVersion = Http.HttpVersion.Http10;
        }
        else
        {
            _httpVersion = Http.HttpVersion.Unknown;
        }
    }

    public IHeaderDictionary RequestHeaders { get; set; } = default!;
    public IHeaderDictionary RequestTrailers { get; } = new HeaderDictionary();
    public bool RequestTrailersAvailable { get; set; }
    public Stream RequestBody { get; set; } = default!;
    public PipeReader RequestBodyPipeReader { get; set; } = default!;
    public HttpResponseTrailers? ResponseTrailers { get; set; }

    private int _statusCode;
    public int StatusCode
    {
        get => _statusCode;
        set
        {
            if (HasResponseStarted)
            {
                ThrowResponseAlreadyStartedException(nameof(StatusCode));
            }

            _statusCode = value;
        }
    }

    private string? _reasonPhrase;

    public string? ReasonPhrase
    {
        get => _reasonPhrase;

        set
        {
            if (HasResponseStarted)
            {
                ThrowResponseAlreadyStartedException(nameof(ReasonPhrase));
            }

            _reasonPhrase = value;
        }
    }

    public IHeaderDictionary ResponseHeaders { get; set; } = default!;
    public Stream ResponseBody { get; set; } = default!;
    public PipeWriter ResponseBodyPipeWriter { get; set; } = default!;

    public CancellationToken RequestAborted
    {
        get
        {
            // If a request abort token was previously explicitly set, return it.
            if (_manuallySetRequestAbortToken.HasValue)
            {
                return _manuallySetRequestAbortToken.Value;
            }

            lock (_abortLock)
            {
                if (_preventRequestAbortedCancellation)
                {
                    return new CancellationToken(false);
                }

                if (_connectionAborted && _abortedCts == null)
                {
                    return new CancellationToken(true);
                }

                if (_abortedCts == null)
                {
                    _abortedCts = new CancellationTokenSource();
                }

                return _abortedCts.Token;
            }
        }
        set
        {
            // Set an abort token, overriding one we create internally.  This setter and associated
            // field exist purely to support IHttpRequestLifetimeFeature.set_RequestAborted.
            _manuallySetRequestAbortToken = value;
        }
    }

    public bool HasResponseStarted => _requestProcessingStatus >= RequestProcessingStatus.HeadersCommitted;

    public bool HasFlushedHeaders => _requestProcessingStatus >= RequestProcessingStatus.HeadersFlushed;

    public bool HasResponseCompleted => _requestProcessingStatus == RequestProcessingStatus.ResponseCompleted;

    protected HttpRequestHeaders HttpRequestHeaders { get; set; } = new HttpRequestHeaders();

    protected HttpResponseHeaders HttpResponseHeaders { get; } = new HttpResponseHeaders();

    public void InitializeBodyControl(MessageBody messageBody)
    {
        if (_bodyControl == null)
        {
            _bodyControl = new BodyControl(bodyControl: this, this);
        }

        (RequestBody, ResponseBody, RequestBodyPipeReader, ResponseBodyPipeWriter) = _bodyControl.Start(messageBody);
        _requestStreamInternal = RequestBody;
        _responseStreamInternal = ResponseBody;
    }

    // For testing
    internal void ResetState()
    {
        _requestProcessingStatus = RequestProcessingStatus.RequestPending;
    }

    public void Reset()
    {
        _onStarting?.Clear();
        _onCompleted?.Clear();
        _routeValues?.Clear();

        _requestProcessingStatus = RequestProcessingStatus.RequestPending;
        _autoChunk = false;
        _applicationException = null;
        _requestRejectedException = null;

        ResetFeatureCollection();

        HasStartedConsumingRequestBody = false;
        MaxRequestBodySize = ServerOptions.Limits.MaxRequestBodySize;
        MinRequestBodyDataRate = ServerOptions.Limits.MinRequestBodyDataRate;
        AllowSynchronousIO = ServerOptions.AllowSynchronousIO;
        TraceIdentifier = null!;
        Method = HttpMethod.None;
        _methodText = null;
        _endpoint = null;
        PathBase = null;
        Path = null;
        RawTarget = null;
        QueryString = null;
        _httpVersion = Http.HttpVersion.Unknown;
        _httpProtocol = null;
        _statusCode = StatusCodes.Status200OK;
        _reasonPhrase = null;
        IsUpgraded = false;
        IsExtendedConnectRequest = false;
        IsExtendedConnectAccepted = false;
        IsWebTransportRequest = false;
        ConnectProtocol = null;

        var remoteEndPoint = RemoteEndPoint;
        RemoteIpAddress = remoteEndPoint?.Address;
        RemotePort = remoteEndPoint?.Port ?? 0;
        var localEndPoint = LocalEndPoint;
        LocalIpAddress = localEndPoint?.Address;
        LocalPort = localEndPoint?.Port ?? 0;

        ConnectionIdFeature = ConnectionId;

        HttpRequestHeaders.Reset();
        HttpRequestHeaders.EncodingSelector = ServerOptions.RequestHeaderEncodingSelector;
        HttpRequestHeaders.ReuseHeaderValues = !ServerOptions.DisableStringReuse;
        HttpResponseHeaders.Reset();
        HttpResponseHeaders.EncodingSelector = ServerOptions.ResponseHeaderEncodingSelector;
        RequestHeaders = HttpRequestHeaders;
        ResponseHeaders = HttpResponseHeaders;
        RequestTrailers.Clear();
        ResponseTrailers?.Reset();
        RequestTrailersAvailable = false;

        _isLeasedMemoryInvalid = true;
        _hasAdvanced = false;
        _canWriteResponseBody = true;

        if (_scheme == null)
        {
            var tlsFeature = ConnectionFeatures?[typeof(ITlsConnectionFeature)];
            _scheme = tlsFeature != null ? SchemeHttps : SchemeHttp;
        }

        Scheme = _scheme;

        _manuallySetRequestAbortToken = null;

        // Lock to prevent CancelRequestAbortedToken from attempting to cancel a disposed CTS.
        CancellationTokenSource? localAbortCts = null;

        lock (_abortLock)
        {
            _preventRequestAbortedCancellation = false;
            if (_abortedCts?.TryReset() == false)
            {
                localAbortCts = _abortedCts;
                _abortedCts = null;
            }
        }

        localAbortCts?.Dispose();

        Output?.Reset();

        _requestHeadersParsed = 0;
        _eagerRequestHeadersParsedLimit = ServerOptions.Limits.MaxRequestHeaderCount;

        _responseBytesWritten = 0;

        OnReset();
    }

    protected abstract void OnReset();

    protected abstract void ApplicationAbort();

    protected virtual void OnRequestProcessingEnding()
    {
    }

    protected virtual void OnRequestProcessingEnded()
    {
    }

    protected virtual void BeginRequestProcessing()
    {
    }

    protected virtual void OnErrorAfterResponseStarted()
    {
    }

    protected virtual bool BeginRead(out ValueTask<ReadResult> awaitable)
    {
        awaitable = default;
        return false;
    }

    protected abstract string CreateRequestId();

    protected abstract MessageBody CreateMessageBody();

    protected abstract bool TryParseRequest(ReadResult result, out bool endConnection);

    private void CancelRequestAbortedTokenCallback()
    {
        try
        {
            CancellationTokenSource? localAbortCts = null;

            lock (_abortLock)
            {
                if (_abortedCts != null && !_preventRequestAbortedCancellation)
                {
                    localAbortCts = _abortedCts;
                }
            }

            // If we cancel the cts, we don't dispose as people may still be using
            // the cts. It also isn't necessary to dispose a canceled cts.
            localAbortCts?.Cancel();
        }
        catch (Exception ex)
        {
            Log.ApplicationError(ConnectionId, TraceIdentifier, ex);
        }
    }

    protected void CancelRequestAbortedToken()
    {
        var shouldScheduleCancellation = false;

        lock (_abortLock)
        {
            if (_connectionAborted)
            {
                return;
            }

            shouldScheduleCancellation = _abortedCts != null && !_preventRequestAbortedCancellation;
            _connectionAborted = true;
        }

        if (shouldScheduleCancellation)
        {
            // Potentially calling user code. CancelRequestAbortedToken logs any exceptions.
            ServiceContext.Scheduler.Schedule(state => ((HttpProtocol)state!).CancelRequestAbortedTokenCallback(), this);
        }
    }

    protected void PoisonBody(Exception abortReason)
    {
        _bodyControl?.Abort(abortReason);
    }

    // Prevents the RequestAborted token from firing for the duration of the request.
    private void PreventRequestAbortedCancellation()
    {
        lock (_abortLock)
        {
            if (_connectionAborted)
            {
                return;
            }

            _preventRequestAbortedCancellation = true;
        }
    }

    public virtual void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, bool checkForNewlineChars)
    {
        IncrementRequestHeadersCount();

        HttpRequestHeaders.Append(name, value, checkForNewlineChars);
    }

    public virtual void OnHeader(int index, bool indexOnly, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        IncrementRequestHeadersCount();

        // This method should be overriden in specific implementations and the base should be
        // called to validate the header count.
    }

    public void OnTrailer(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        IncrementRequestHeadersCount();

        string key = name.GetHeaderName();
        var valueStr = value.GetRequestHeaderString(key, HttpRequestHeaders.EncodingSelector, checkForNewlineChars: false);
        RequestTrailers.Append(key, valueStr);
    }

    private void IncrementRequestHeadersCount()
    {
        _requestHeadersParsed++;
        if (_requestHeadersParsed > _eagerRequestHeadersParsedLimit)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.TooManyHeaders);
        }
    }

    public void OnHeadersComplete()
    {
        HttpRequestHeaders.OnHeadersComplete();
    }

    public void OnTrailersComplete()
    {
        RequestTrailersAvailable = true;
    }

    public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        try
        {
            // We run the request processing loop in a seperate async method so per connection
            // exception handling doesn't complicate the generated asm for the loop.
            await ProcessRequests(application);
        }
        catch (BadHttpRequestException ex)
        {
            // Handle BadHttpRequestException thrown during request line or header parsing.
            // SetBadRequestState logs the error.
            SetBadRequestState(ex);
        }
        catch (ConnectionResetException ex)
        {
            // Don't log ECONNRESET errors made between requests. Browsers like IE will reset connections regularly.
            if (_requestProcessingStatus != RequestProcessingStatus.RequestPending)
            {
                Log.RequestProcessingError(ConnectionId, ex);
            }
        }
        catch (IOException ex)
        {
            Log.RequestProcessingError(ConnectionId, ex);
        }
        catch (ConnectionAbortedException ex)
        {
            Log.RequestProcessingError(ConnectionId, ex);
        }
        catch (Exception ex)
        {
            Log.LogWarning(0, ex, CoreStrings.RequestProcessingEndError);
        }
        finally
        {
            try
            {
                if (_requestRejectedException != null)
                {
                    await TryProduceInvalidRequestResponse();
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning(0, ex, CoreStrings.ConnectionShutdownError);
            }
            finally
            {
                OnRequestProcessingEnded();
            }
        }
    }

    private async Task ProcessRequests<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        while (_keepAlive)
        {
            if (_context.InitialExecutionContext is null)
            {
                // If this is a first request on a non-Http2Connection, capture a clean ExecutionContext.
                _context.InitialExecutionContext = ExecutionContext.Capture();
            }
            else
            {
                // Clear any AsyncLocals set during the request; back to a clean state ready for next request
                // And/or reset to Http2Connection's ExecutionContext giving access to the connection logging scope
                // and any other AsyncLocals set by connection middleware.
                ExecutionContext.Restore(_context.InitialExecutionContext);
            }

            BeginRequestProcessing();

            var result = default(ReadResult);
            bool endConnection;
            do
            {
                if (BeginRead(out var awaitable))
                {
                    result = await awaitable;
                }
            } while (!TryParseRequest(result, out endConnection));

            if (endConnection)
            {
                // Connection finished, stop processing requests
                return;
            }

            var messageBody = CreateMessageBody();
            if (!messageBody.RequestKeepAlive)
            {
                DisableKeepAlive(ConnectionEndReason.RequestNoKeepAlive);
            }

            IsUpgradableRequest = messageBody.RequestUpgrade;

            InitializeBodyControl(messageBody);

            var context = application.CreateContext(this);

            try
            {
                KestrelEventSource.Log.RequestStart(this);

                // Run the application code for this request
                await application.ProcessRequestAsync(context);

                // Trigger OnStarting if it hasn't been called yet and the app hasn't
                // already failed. If an OnStarting callback throws we can go through
                // our normal error handling in ProduceEnd.
                // https://github.com/aspnet/KestrelHttpServer/issues/43
                if (!HasResponseStarted && _applicationException == null && _onStarting?.Count > 0)
                {
                    await FireOnStarting();
                }

                if (!_connectionAborted && !VerifyResponseContentLength(out var lengthException))
                {
                    ReportApplicationError(lengthException);
                }
            }
            catch (BadHttpRequestException ex)
            {
                // Capture BadHttpRequestException for further processing
                // This has to be caught here so StatusCode is set properly before disposing the HttpContext
                // (DisposeContext logs StatusCode).
                SetBadRequestState(ex);
                ReportApplicationError(ex);
            }
            catch (Exception ex)
            {
                if ((ex is OperationCanceledException || ex is IOException) && _connectionAborted)
                {
                    Log.RequestAborted(ConnectionId, TraceIdentifier);
                }
                else
                {
                    ReportApplicationError(ex);
                }
            }

            KestrelEventSource.Log.RequestStop(this);

            // At this point all user code that needs use to the request or response streams has completed.
            // Using these streams in the OnCompleted callback is not allowed.
            try
            {
                Debug.Assert(_bodyControl != null);
                await _bodyControl.StopAsync();
            }
            catch (Exception ex)
            {
                // BodyControl.StopAsync() can throw if the PipeWriter was completed prior to the application writing
                // enough bytes to satisfy the specified Content-Length. This risks double-logging the exception,
                // but this scenario generally indicates an app bug, so I don't want to risk not logging it.
                ReportApplicationError(ex);
            }

            // 4XX responses are written by TryProduceInvalidRequestResponse during connection tear down.
            if (_requestRejectedException == null)
            {
                if (!_connectionAborted)
                {
                    // Call ProduceEnd() before consuming the rest of the request body to prevent
                    // delaying clients waiting for the chunk terminator:
                    //
                    // https://github.com/dotnet/corefx/issues/17330#issuecomment-288248663
                    //
                    // This also prevents the 100 Continue response from being sent if the app
                    // never tried to read the body.
                    // https://github.com/aspnet/KestrelHttpServer/issues/2102
                    //
                    // ProduceEnd() must be called before _application.DisposeContext(), to ensure
                    // HttpContext.Response.StatusCode is correctly set when
                    // IHttpContextFactory.Dispose(HttpContext) is called.
                    await ProduceEnd();
                }
                else if (!HasResponseStarted)
                {
                    // If the request was aborted and no response was sent, we use status code 499 for logging                    
                    StatusCode = StatusCodes.Status499ClientClosedRequest;
                }
            }

            if (_onCompleted?.Count > 0)
            {
                await FireOnCompleted();
            }

            application.DisposeContext(context, _applicationException);

            // Even for non-keep-alive requests, try to consume the entire body to avoid RSTs.
            if (!_connectionAborted && _requestRejectedException == null && !messageBody.IsEmpty)
            {
                await messageBody.ConsumeAsync();
            }

            if (HasStartedConsumingRequestBody)
            {
                await messageBody.StopAsync();
            }
        }
    }

    public void OnStarting(Func<object, Task> callback, object state)
    {
        if (HasResponseStarted)
        {
            ThrowResponseAlreadyStartedException(nameof(OnStarting));
        }

        if (_onStarting == null)
        {
            _onStarting = new Stack<KeyValuePair<Func<object, Task>, object>>();
        }
        _onStarting.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
        if (_onCompleted == null)
        {
            _onCompleted = new Stack<KeyValuePair<Func<object, Task>, object>>();
        }
        _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
    }

    protected Task FireOnStarting()
    {
        var onStarting = _onStarting;
        if (onStarting?.Count > 0)
        {
            return ProcessEvents(this, onStarting);
        }

        return Task.CompletedTask;

        static async Task ProcessEvents(HttpProtocol protocol, Stack<KeyValuePair<Func<object, Task>, object>> events)
        {
            // Try/Catch is outside the loop as any error that occurs is before the request starts.
            // So we want to report it as an ApplicationError to fail the request and not process more events.
            try
            {
                while (events.TryPop(out var entry))
                {
                    await entry.Key.Invoke(entry.Value);
                }
            }
            catch (Exception ex)
            {
                protocol.ReportApplicationError(ex);
            }
        }
    }

    protected Task FireOnCompleted()
    {
        var onCompleted = _onCompleted;
        if (onCompleted?.Count > 0)
        {
            return ProcessEvents(this, onCompleted);
        }

        return Task.CompletedTask;

        static async Task ProcessEvents(HttpProtocol protocol, Stack<KeyValuePair<Func<object, Task>, object>> events)
        {
            // Try/Catch is inside the loop as any error that occurs is after the request has finished.
            // So we will just log it and keep processing the events, as the completion has already happened.
            while (events.TryPop(out var entry))
            {
                try
                {
                    await entry.Key.Invoke(entry.Value);
                }
                catch (Exception ex)
                {
                    protocol.Log.ApplicationError(protocol.ConnectionId, protocol.TraceIdentifier, ex);
                }
            }
        }
    }

    private void VerifyAndUpdateWrite(int count)
    {
        var responseHeaders = HttpResponseHeaders;

        if (responseHeaders != null &&
            !responseHeaders.HasTransferEncoding &&
            responseHeaders.ContentLength.HasValue &&
            _responseBytesWritten + count > responseHeaders.ContentLength.Value)
        {
            DisableKeepAlive(ConnectionEndReason.ResponseContentLengthMismatch);
            ThrowTooManyBytesWritten(count);
        }

        _responseBytesWritten += count;
    }

    [StackTraceHidden]
    private void ThrowTooManyBytesWritten(int count)
    {
        throw GetTooManyBytesWrittenException(count);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private InvalidOperationException GetTooManyBytesWrittenException(int count)
    {
        var responseHeaders = HttpResponseHeaders;
        return new InvalidOperationException(
            CoreStrings.FormatTooManyBytesWritten(_responseBytesWritten + count, responseHeaders.ContentLength!.Value));
    }

    private void CheckLastWrite()
    {
        var responseHeaders = HttpResponseHeaders;

        // Prevent firing request aborted token if this is the last write, to avoid
        // aborting the request if the app is still running when the client receives
        // the final bytes of the response and gracefully closes the connection.
        //
        // Called after VerifyAndUpdateWrite(), so _responseBytesWritten has already been updated.
        if (responseHeaders != null &&
            !responseHeaders.HasTransferEncoding &&
            responseHeaders.ContentLength.HasValue &&
            _responseBytesWritten == responseHeaders.ContentLength.Value)
        {
            PreventRequestAbortedCancellation();
        }
    }

    protected bool VerifyResponseContentLength([NotNullWhen(false)] out Exception? ex)
    {
        var responseHeaders = HttpResponseHeaders;

        if (Method != HttpMethod.Head &&
            StatusCode != StatusCodes.Status304NotModified &&
            !responseHeaders.HasTransferEncoding &&
            responseHeaders.ContentLength.HasValue &&
            _responseBytesWritten < responseHeaders.ContentLength.Value)
        {
            // We need to close the connection if any bytes were written since the client
            // cannot be certain of how many bytes it will receive.
            if (_responseBytesWritten > 0)
            {
                DisableKeepAlive(ConnectionEndReason.ResponseContentLengthMismatch);
            }

            ex = new InvalidOperationException(
                CoreStrings.FormatTooFewBytesWritten(_responseBytesWritten, responseHeaders.ContentLength.Value));
            return false;
        }

        ex = null;
        return true;
    }

    public ValueTask<FlushResult> ProduceContinueAsync()
    {
        if (HasResponseStarted)
        {
            return default;
        }

        if (_httpVersion != Http.HttpVersion.Http10 &&
            ((IHeaderDictionary)HttpRequestHeaders).TryGetValue(HeaderNames.Expect, out var expect) &&
            (expect.FirstOrDefault() ?? "").Equals("100-continue", StringComparison.OrdinalIgnoreCase))
        {
            return Output.Write100ContinueAsync();
        }

        return default;
    }

    public Task InitializeResponseAsync(int firstWriteByteCount)
    {
        var startingTask = FireOnStarting();
        if (!startingTask.IsCompletedSuccessfully)
        {
            return InitializeResponseAwaited(startingTask, firstWriteByteCount);
        }

        VerifyInitializeState(firstWriteByteCount);

        ProduceStart(appCompleted: false);

        return Task.CompletedTask;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task InitializeResponseAwaited(Task startingTask, int firstWriteByteCount)
    {
        await startingTask;

        VerifyInitializeState(firstWriteByteCount);

        ProduceStart(appCompleted: false);
    }

    private HttpResponseHeaders InitializeResponseFirstWrite(int firstWriteByteCount)
    {
        VerifyInitializeState(firstWriteByteCount);

        var responseHeaders = CreateResponseHeaders(appCompleted: false);

        // InitializeResponse can only be called if we are just about to Flush the headers
        _requestProcessingStatus = RequestProcessingStatus.HeadersFlushed;

        return responseHeaders;
    }

    private void ProduceStart(bool appCompleted)
    {
        if (HasResponseStarted)
        {
            return;
        }

        _isLeasedMemoryInvalid = true;

        _requestProcessingStatus = RequestProcessingStatus.HeadersCommitted;

        var responseHeaders = CreateResponseHeaders(appCompleted);

        Output.WriteResponseHeaders(StatusCode, ReasonPhrase, responseHeaders, _autoChunk, appCompleted);
    }

    private void VerifyInitializeState(int firstWriteByteCount)
    {
        if (_applicationException != null)
        {
            ThrowResponseAbortedException();
        }

        VerifyAndUpdateWrite(firstWriteByteCount);
    }

    protected virtual Task TryProduceInvalidRequestResponse()
    {
        Debug.Assert(_requestRejectedException != null);

        // If _connectionAborted is set, the connection has already been closed.
        if (!_connectionAborted)
        {
            return ProduceEnd();
        }

        return Task.CompletedTask;
    }

    protected Task ProduceEnd()
    {
        if (HasResponseCompleted)
        {
            return Task.CompletedTask;
        }

        _isLeasedMemoryInvalid = true;

        if (_requestRejectedException != null || _applicationException != null)
        {
            if (HasResponseStarted)
            {
                // We can no longer change the response, so we simply close the connection.
                DisableKeepAlive(ConnectionEndReason.ErrorAfterStartingResponse);
                OnErrorAfterResponseStarted();
                return Task.CompletedTask;
            }

            // If the request was rejected, the error state has already been set by SetBadRequestState and
            // that should take precedence.
            if (_requestRejectedException != null)
            {
                SetErrorResponseException(_requestRejectedException);
            }
            else
            {
                // 500 Internal Server Error
                SetErrorResponseHeaders(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        if (!HasResponseStarted)
        {
            ProduceStart(appCompleted: true);
        }

        return WriteSuffix();
    }

    private Task WriteSuffix()
    {
        if (_autoChunk || _httpVersion >= Http.HttpVersion.Http2)
        {
            // For the same reason we call CheckLastWrite() in Content-Length responses.
            PreventRequestAbortedCancellation();
        }

        var writeTask = Output.WriteStreamSuffixAsync();

        if (!writeTask.IsCompletedSuccessfully)
        {
            return WriteSuffixAwaited(writeTask);
        }

        writeTask.GetAwaiter().GetResult();

        _requestProcessingStatus = RequestProcessingStatus.ResponseCompleted;

        if (_keepAlive)
        {
            Log.ConnectionKeepAlive(ConnectionId);
        }

        if (Method == HttpMethod.Head && _responseBytesWritten > 0)
        {
            Log.ConnectionHeadResponseBodyWrite(ConnectionId, _responseBytesWritten);
        }

        return Task.CompletedTask;
    }

    private async Task WriteSuffixAwaited(ValueTask<FlushResult> writeTask)
    {
        _requestProcessingStatus = RequestProcessingStatus.HeadersFlushed;

        await writeTask;

        _requestProcessingStatus = RequestProcessingStatus.ResponseCompleted;

        if (_keepAlive)
        {
            Log.ConnectionKeepAlive(ConnectionId);
        }

        if (Method == HttpMethod.Head && _responseBytesWritten > 0)
        {
            Log.ConnectionHeadResponseBodyWrite(ConnectionId, _responseBytesWritten);
        }
    }

    private HttpResponseHeaders CreateResponseHeaders(bool appCompleted)
    {
        var responseHeaders = HttpResponseHeaders;
        var hasConnection = responseHeaders.HasConnection;
        var hasTransferEncoding = responseHeaders.HasTransferEncoding;

        // We opt to remove the following headers from an HTTP/2+ response since their presence would be considered a protocol violation.
        // This is done quietly because these headers are valid in other contexts and this saves the app from being broken by
        // low level protocol details. Http.Sys also removes these headers silently.
        //
        // https://tools.ietf.org/html/rfc7540#section-8.1.2.2
        // "This means that an intermediary transforming an HTTP/1.x message to HTTP/2 will need to remove any header fields
        // nominated by the Connection header field, along with the Connection header field itself.
        // Such intermediaries SHOULD also remove other connection-specific header fields, such as Keep-Alive,
        // Proxy-Connection, Transfer-Encoding, and Upgrade, even if they are not nominated by the Connection header field."
        //
        // Http/3 has a similar requirement: https://quicwg.org/base-drafts/draft-ietf-quic-http.html#name-field-formatting-and-compre
        if (_httpVersion > Http.HttpVersion.Http11 && responseHeaders.HasInvalidH2H3Headers)
        {
            responseHeaders.ClearInvalidH2H3Headers();
            hasTransferEncoding = false;
            hasConnection = false;

            Log.InvalidResponseHeaderRemoved();
        }

        if (_keepAlive &&
            hasConnection &&
            (HttpHeaders.ParseConnection(responseHeaders) & ConnectionOptions.KeepAlive) == 0)
        {
            DisableKeepAlive(ConnectionEndReason.ResponseNoKeepAlive);
        }

        // https://tools.ietf.org/html/rfc7230#section-3.3.1
        // If any transfer coding other than
        // chunked is applied to a response payload body, the sender MUST either
        // apply chunked as the final transfer coding or terminate the message
        // by closing the connection.
        if (hasTransferEncoding &&
            HttpHeaders.GetFinalTransferCoding(responseHeaders.HeaderTransferEncoding) != TransferCoding.Chunked)
        {
            DisableKeepAlive(ConnectionEndReason.ResponseNoKeepAlive);
        }

        // Set whether response can have body
        _canWriteResponseBody = CanWriteResponseBody();

        if (!_canWriteResponseBody && hasTransferEncoding)
        {
            RejectInvalidHeaderForNonBodyResponse(appCompleted, HeaderNames.TransferEncoding);
        }
        else if (responseHeaders.ContentLength.HasValue)
        {
            if (!CanIncludeResponseContentLengthHeader())
            {
                if (responseHeaders.ContentLength.Value == 0)
                {
                    // If the response shouldn't include a Content-Length but it's 0
                    // we'll just get rid of it without throwing an error, since it
                    // is semantically equivalent to not having a Content-Length.
                    responseHeaders.ContentLength = null;
                }
                else
                {
                    RejectInvalidHeaderForNonBodyResponse(appCompleted, HeaderNames.ContentLength);
                }
            }
            else if (StatusCode == StatusCodes.Status205ResetContent && responseHeaders.ContentLength.Value != 0)
            {
                // It is valid for a 205 response to have a Content-Length but it must be 0
                // since 205 implies that no additional content will be provided.
                // https://httpwg.org/specs/rfc7231.html#rfc.section.6.3.6
                RejectNonzeroContentLengthOn205Response(appCompleted);
            }
        }
        else if (StatusCode == StatusCodes.Status101SwitchingProtocols)
        {
            DisableKeepAlive(ConnectionEndReason.ResponseNoKeepAlive);
        }
        else if (!hasTransferEncoding && !responseHeaders.ContentLength.HasValue)
        {
            if ((appCompleted || !_canWriteResponseBody) && !_hasAdvanced) // Avoid setting contentLength of 0 if we wrote data before calling CreateResponseHeaders
            {
                if (CanAutoSetContentLengthZeroResponseHeader())
                {
                    // Since the app has completed writing or cannot write to the response, we can safely set the Content-Length to 0.
                    responseHeaders.ContentLength = 0;
                }
            }
            // Note for future reference: never change this to set _autoChunk to true on HTTP/1.0
            // connections, even if we were to infer the client supports it because an HTTP/1.0 request
            // was received that used chunked encoding. Sending a chunked response to an HTTP/1.0
            // client would break compliance with RFC 7230 (section 3.3.1):
            //
            // A server MUST NOT send a response containing Transfer-Encoding unless the corresponding
            // request indicates HTTP/1.1 (or later).
            //
            // This also covers HTTP/2, which forbids chunked encoding in RFC 7540 (section 8.1:
            //
            // The chunked transfer encoding defined in Section 4.1 of [RFC7230] MUST NOT be used in HTTP/2.
            else if (_httpVersion == Http.HttpVersion.Http11)
            {
                _autoChunk = true;
                responseHeaders.SetRawTransferEncoding("chunked", _bytesTransferEncodingChunked);
            }
            else
            {
                DisableKeepAlive(ConnectionEndReason.ResponseNoKeepAlive);
            }
        }

        responseHeaders.SetReadOnly();

        if (!hasConnection && _httpVersion < Http.HttpVersion.Http2)
        {
            if (!_keepAlive)
            {
                responseHeaders.SetRawConnection("close", _bytesConnectionClose);
            }
            else if (_httpVersion == Http.HttpVersion.Http10)
            {
                responseHeaders.SetRawConnection("keep-alive", _bytesConnectionKeepAlive);
            }
        }

        if (_context.AltSvcHeader != null && !responseHeaders.HasAltSvc)
        {
            responseHeaders.SetRawAltSvc(_context.AltSvcHeader.Value, _context.AltSvcHeader.RawBytes);
        }

        if (ServerOptions.AddServerHeader && !responseHeaders.HasServer)
        {
            responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
        }

        if (!responseHeaders.HasDate)
        {
            var dateHeaderValues = DateHeaderValueManager.GetDateHeaderValues();
            responseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
        }

        return responseHeaders;
    }

    private bool CanIncludeResponseContentLengthHeader()
    {
        // Section 4.3.6 of RFC7231
        if (Is1xxCode(StatusCode) || StatusCode == StatusCodes.Status204NoContent)
        {
            // A server MUST NOT send a Content-Length header field in any response
            // with a status code of 1xx (Informational) or 204 (No Content).
            return false;
        }
        else if (Method == HttpMethod.Connect && Is2xxCode(StatusCode))
        {
            // A server MUST NOT send a Content-Length header field in any 2xx
            // (Successful) response to a CONNECT request.
            return false;
        }

        return true;

        static bool Is1xxCode(int code) => code >= StatusCodes.Status100Continue && code < StatusCodes.Status200OK;
        static bool Is2xxCode(int code) => code >= StatusCodes.Status200OK && code < StatusCodes.Status300MultipleChoices;
    }

    private bool CanWriteResponseBody()
    {
        // List of status codes taken from Microsoft.Net.Http.Server.Response
        return Method != HttpMethod.Head &&
               StatusCode != StatusCodes.Status204NoContent &&
               StatusCode != StatusCodes.Status205ResetContent &&
               StatusCode != StatusCodes.Status304NotModified;
    }

    private bool CanAutoSetContentLengthZeroResponseHeader()
    {
        return CanIncludeResponseContentLengthHeader() &&
            // Responses to HEAD may omit Content-Length (Section 4.3.6 of RFC7231).
            Method != HttpMethod.Head &&
            // 304s should only include specific fields, of which Content-Length is
            // not one (Section 4.1 of RFC7232).
            StatusCode != StatusCodes.Status304NotModified;
    }

    private static void ThrowResponseAlreadyStartedException(string value)
    {
        throw new InvalidOperationException(CoreStrings.FormatParameterReadOnlyAfterResponseStarted(value));
    }

    private void RejectInvalidHeaderForNonBodyResponse(bool appCompleted, string headerName)
        => RejectInvalidResponse(appCompleted, CoreStrings.FormatHeaderNotAllowedOnResponse(headerName, StatusCode));

    private void RejectNonzeroContentLengthOn205Response(bool appCompleted)
        => RejectInvalidResponse(appCompleted, CoreStrings.NonzeroContentLengthNotAllowedOn205);

    private void RejectInvalidResponse(bool appCompleted, string message)
    {
        var ex = new InvalidOperationException(message);
        if (!appCompleted)
        {
            // Back out of header creation surface exception in user code
            _requestProcessingStatus = RequestProcessingStatus.AppStarted;
            throw ex;
        }
        else
        {
            ReportApplicationError(ex);

            // 500 Internal Server Error
            SetErrorResponseHeaders(statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private void SetErrorResponseException(BadHttpRequestException ex)
    {
        SetErrorResponseHeaders(ex.StatusCode);

#pragma warning disable CS0618 // Type or member is obsolete
        if (ex is Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException kestrelEx && !StringValues.IsNullOrEmpty(kestrelEx.AllowedHeader))
#pragma warning restore CS0618 // Type or member is obsolete
        {
            HttpResponseHeaders.HeaderAllow = kestrelEx.AllowedHeader;
        }
    }

    private void SetErrorResponseHeaders(int statusCode)
    {
        Debug.Assert(!HasResponseStarted, $"{nameof(SetErrorResponseHeaders)} called after response had already started.");

        StatusCode = statusCode;
        ReasonPhrase = null;

        var responseHeaders = HttpResponseHeaders;
        responseHeaders.Reset();
        ResponseTrailers?.Reset();
        var dateHeaderValues = DateHeaderValueManager.GetDateHeaderValues();

        responseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);

        responseHeaders.ContentLength = 0;

        if (ServerOptions.AddServerHeader)
        {
            responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
        }
    }

    public void HandleNonBodyResponseWrite()
    {
        // Writes to HEAD response are ignored and logged at the end of the request
        if (Method != HttpMethod.Head)
        {
            ThrowWritingToResponseBodyNotSupported();
        }
    }

    [StackTraceHidden]
    private void ThrowWritingToResponseBodyNotSupported()
    {
        // Throw Exception for 204, 205, 304 responses.
        throw new InvalidOperationException(CoreStrings.FormatWritingToResponseBodyNotSupported(StatusCode));
    }

    [StackTraceHidden]
    private void ThrowResponseAbortedException()
    {
        throw new ObjectDisposedException(CoreStrings.UnhandledApplicationException, _applicationException);
    }

    [StackTraceHidden]
    [DoesNotReturn]
    public void ThrowRequestTargetRejected(Span<byte> target)
        => throw GetInvalidRequestTargetException(target);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BadHttpRequestException GetInvalidRequestTargetException(ReadOnlySpan<byte> target)
        => KestrelBadHttpRequestException.GetException(
            RequestRejectionReason.InvalidRequestTarget,
            Log.IsEnabled(LogLevel.Information)
                ? target.GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                : string.Empty);

    public void SetBadRequestState(BadHttpRequestException ex)
    {
        Log.ConnectionBadRequest(ConnectionId, ex);
        _requestRejectedException = ex;

        if (!HasResponseStarted)
        {
            SetErrorResponseException(ex);
        }

        const string badRequestEventName = "Microsoft.AspNetCore.Server.Kestrel.BadRequest";
        if (ServiceContext.DiagnosticSource?.IsEnabled(badRequestEventName) == true)
        {
            WriteDiagnosticEvent(ServiceContext.DiagnosticSource, badRequestEventName, this);
        }

        DisableKeepAlive(Http1Connection.GetConnectionEndReason(ex));
    }

    internal virtual void DisableKeepAlive(ConnectionEndReason reason)
    {
        _keepAlive = false;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
        Justification = "The values being passed into Write are being consumed by the application already.")]
    private static void WriteDiagnosticEvent(DiagnosticSource diagnosticSource, string name, HttpProtocol value)
    {
        diagnosticSource.Write(name, value);
    }

    public void ReportApplicationError(Exception? ex)
    {
        // ReportApplicationError can be called with a null exception from MessageBody
        if (ex == null)
        {
            return;
        }

        if (_applicationException == null)
        {
            _applicationException = ex;
        }
        else if (_applicationException is AggregateException)
        {
            _applicationException = new AggregateException(_applicationException, ex).Flatten();
        }
        else
        {
            _applicationException = new AggregateException(_applicationException, ex);
        }

        Log.ApplicationError(ConnectionId, TraceIdentifier, ex);
    }

    public void Advance(int bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes));
        }
        else if (bytes > 0)
        {
            _hasAdvanced = true;
        }

        if (_isLeasedMemoryInvalid)
        {
            throw new InvalidOperationException("Invalid ordering of calling StartAsync or CompleteAsync and Advance.");
        }

        if (_canWriteResponseBody)
        {
            VerifyAndUpdateWrite(bytes);
            Output.Advance(bytes);
        }
        else
        {
            HandleNonBodyResponseWrite();
            // For HEAD requests, we still use the number of bytes written for logging
            // how many bytes were written.
            VerifyAndUpdateWrite(bytes);
        }
    }

    public long UnflushedBytes => Output.UnflushedBytes;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        _isLeasedMemoryInvalid = false;
        return Output.GetMemory(sizeHint);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        _isLeasedMemoryInvalid = false;
        return Output.GetSpan(sizeHint);
    }

    public ValueTask<FlushResult> FlushPipeAsync(CancellationToken cancellationToken)
    {
        if (!HasResponseStarted)
        {
            var initializeTask = InitializeResponseAsync(0);
            if (!initializeTask.IsCompletedSuccessfully)
            {
                return FlushAsyncAwaited(initializeTask, cancellationToken);
            }
        }

        return Output.FlushAsync(cancellationToken);
    }

    public void CancelPendingFlush()
    {
        Output.CancelPendingFlush();
    }

    public Task CompleteAsync(Exception? exception = null)
    {
        if (exception != null)
        {
            var wrappedException = new ConnectionAbortedException("The BodyPipe was completed with an exception.", exception);
            ReportApplicationError(wrappedException);

            if (HasResponseStarted)
            {
                ApplicationAbort();
            }
        }

        // Finalize headers
        if (!HasResponseStarted)
        {
            var onStartingTask = FireOnStarting();
            if (!onStartingTask.IsCompletedSuccessfully)
            {
                return CompleteAsyncAwaited(onStartingTask);
            }
        }

        // Flush headers, body, trailers...
        if (!HasResponseCompleted)
        {
            if (!VerifyResponseContentLength(out var lengthException))
            {
                // Try to throw this exception from CompleteAsync() instead of CompleteAsyncAwaited() if possible,
                // so it can be observed by BodyWriter.Complete(). If this isn't possible because an
                // async OnStarting callback hadn't yet run, it's OK, since the Exception will be observed with
                // the call to _bodyControl.StopAsync() in ProcessRequests().
                ThrowException(lengthException);
            }

            return ProduceEnd();
        }

        return Task.CompletedTask;
    }

    private async Task CompleteAsyncAwaited(Task onStartingTask)
    {
        await onStartingTask;

        if (!HasResponseCompleted)
        {
            if (!VerifyResponseContentLength(out var lengthException))
            {
                ThrowException(lengthException);
            }

            await ProduceEnd();
        }
    }

    [StackTraceHidden]
    private static void ThrowException(Exception exception)
    {
        throw exception;
    }

    public ValueTask<FlushResult> WritePipeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        // For the first write, ensure headers are flushed if WriteDataAsync isn't called.
        if (!HasResponseStarted)
        {
            return FirstWriteAsync(data, cancellationToken);
        }
        else
        {
            VerifyAndUpdateWrite(data.Length);
        }

        if (_canWriteResponseBody)
        {
            if (_autoChunk)
            {
                if (data.Length == 0)
                {
                    return default;
                }

                return Output.WriteChunkAsync(data.Span, cancellationToken);
            }
            else
            {
                CheckLastWrite();
                return Output.WriteDataToPipeAsync(data.Span, cancellationToken: cancellationToken);
            }
        }
        else
        {
            HandleNonBodyResponseWrite();
            return default;
        }
    }

    private ValueTask<FlushResult> FirstWriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        Debug.Assert(!HasResponseStarted);

        var startingTask = FireOnStarting();
        if (!startingTask.IsCompletedSuccessfully)
        {
            return FirstWriteAsyncAwaited(startingTask, data, cancellationToken);
        }

        return FirstWriteAsyncInternal(data, cancellationToken);
    }

    private async ValueTask<FlushResult> FirstWriteAsyncAwaited(Task initializeTask, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await initializeTask;

        return await FirstWriteAsyncInternal(data, cancellationToken);
    }

    private ValueTask<FlushResult> FirstWriteAsyncInternal(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var responseHeaders = InitializeResponseFirstWrite(data.Length);

        if (_canWriteResponseBody)
        {
            if (_autoChunk)
            {
                if (data.Length == 0)
                {
                    Output.WriteResponseHeaders(StatusCode, ReasonPhrase, responseHeaders, _autoChunk, appCompleted: false);
                    return Output.FlushAsync(cancellationToken);
                }

                return Output.FirstWriteChunkedAsync(StatusCode, ReasonPhrase, responseHeaders, _autoChunk, data.Span, cancellationToken);
            }
            else
            {
                CheckLastWrite();
                return Output.FirstWriteAsync(StatusCode, ReasonPhrase, responseHeaders, _autoChunk, data.Span, cancellationToken);
            }
        }
        else
        {
            Output.WriteResponseHeaders(StatusCode, ReasonPhrase, responseHeaders, _autoChunk, appCompleted: false);
            HandleNonBodyResponseWrite();
            return Output.FlushAsync(cancellationToken);
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return FlushPipeAsync(cancellationToken).GetAsTask();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async ValueTask<FlushResult> FlushAsyncAwaited(Task initializeTask, CancellationToken cancellationToken)
    {
        await initializeTask;
        return await Output.FlushAsync(cancellationToken);
    }

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        return WritePipeAsync(data, cancellationToken).GetAsTask();
    }

    public async ValueTask<FlushResult> WriteAsyncAwaited(Task initializeTask, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await initializeTask;

        // WriteAsyncAwaited is only called for the first write to the body.
        // Ensure headers are flushed if Write(Chunked)Async isn't called.
        if (_canWriteResponseBody)
        {
            if (_autoChunk)
            {
                if (data.Length == 0)
                {
                    return await Output.FlushAsync(cancellationToken);
                }

                return await Output.WriteChunkAsync(data.Span, cancellationToken);
            }
            else
            {
                CheckLastWrite();
                return await Output.WriteDataToPipeAsync(data.Span, cancellationToken: cancellationToken);
            }
        }
        else
        {
            HandleNonBodyResponseWrite();
            return await Output.FlushAsync(cancellationToken);
        }
    }
}
