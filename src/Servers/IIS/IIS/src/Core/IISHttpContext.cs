// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.Server.IIS.Core.IO;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.IIS.Core;

using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

internal abstract partial class IISHttpContext : NativeRequestContext, IThreadPoolWorkItem, IDisposable
{
    private const int MinAllocBufferSize = 2048;

    protected readonly NativeSafeHandle _requestNativeHandle;

    private readonly IISServerOptions _options;

    protected Streams _streams = default!;

    private volatile bool _hasResponseStarted;

    private int _statusCode;
    private string? _reasonPhrase;
    // Used to synchronize callback registration and native method calls
    internal readonly object _contextLock = new object();

    protected Stack<KeyValuePair<Func<object, Task>, object>>? _onStarting;
    protected Stack<KeyValuePair<Func<object, Task>, object>>? _onCompleted;

    protected Exception? _applicationException;
    protected BadHttpRequestException? _requestRejectedException;

    private readonly MemoryPool<byte> _memoryPool;
    private readonly IISHttpServer _server;

    private readonly ILogger _logger;

    private GCHandle _thisHandle = default!;
    protected Task? _readBodyTask;
    protected Task? _writeBodyTask;

    private bool _wasUpgraded;

    protected Pipe? _bodyInputPipe;
    protected OutputProducer _bodyOutput = default!;

    private HeaderCollection? _trailers;

    private const string NtlmString = "NTLM";
    private const string NegotiateString = "Negotiate";
    private const string BasicString = "Basic";
    private const string ConnectionClose = "close";

    internal unsafe IISHttpContext(
        MemoryPool<byte> memoryPool,
        NativeSafeHandle pInProcessHandler,
        IISServerOptions options,
        IISHttpServer server,
        ILogger logger,
        bool useLatin1)
        : base((HttpApiTypes.HTTP_REQUEST*)NativeMethods.HttpGetRawRequest(pInProcessHandler), useLatin1: useLatin1)
    {
        _memoryPool = memoryPool;
        _requestNativeHandle = pInProcessHandler;
        _options = options;
        _server = server;
        _logger = logger;

        ((IHttpBodyControlFeature)this).AllowSynchronousIO = _options.AllowSynchronousIO;
    }

    private int PauseWriterThreshold => _options.MaxRequestBodyBufferSize;
    private int ResumeWriterTheshold => PauseWriterThreshold / 2;

    public Version HttpVersion { get; set; } = default!;
    public string Scheme { get; set; } = default!;
    public string Method { get; set; } = default!;
    public string PathBase { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string QueryString { get; set; } = default!;
    public string RawTarget { get; set; } = default!;

    public bool HasResponseStarted => _hasResponseStarted;
    public IPAddress? RemoteIpAddress { get; set; }
    public int RemotePort { get; set; }
    public IPAddress? LocalIpAddress { get; set; }
    public int LocalPort { get; set; }
    public string? RequestConnectionId { get; set; }
    public string? TraceIdentifier { get; set; }
    public ClaimsPrincipal? User { get; set; }
    internal WindowsPrincipal? WindowsUser { get; set; }
    internal bool RequestCanHaveBody { get; private set; }
    public Stream RequestBody { get; set; } = default!;
    public Stream ResponseBody { get; set; } = default!;
    public PipeWriter? ResponsePipeWrapper { get; set; }

    protected IAsyncIOEngine? AsyncIO { get; set; }

    public IHeaderDictionary RequestHeaders { get; set; } = default!;
    public IHeaderDictionary ResponseHeaders { get; set; } = default!;
    public IHeaderDictionary? ResponseTrailers { get; set; }
    private HeaderCollection HttpResponseHeaders { get; set; } = default!;
    private HeaderCollection HttpResponseTrailers => _trailers ??= new HeaderCollection(checkTrailers: true);
    internal bool HasTrailers => _trailers?.Count > 0;

    internal HttpApiTypes.HTTP_VERB KnownMethod { get; private set; }

    private bool HasStartedConsumingRequestBody { get; set; }
    public long? MaxRequestBodySize { get; set; }

    protected void InitializeContext()
    {
        // create a memory barrier between initialize and disconnect to prevent a possible
        // NullRef with disconnect being called before these fields have been written
        // disconnect aquires this lock as well
        lock (_abortLock)
        {
            _thisHandle = GCHandle.Alloc(this);

            Method = GetVerb() ?? string.Empty;

            RawTarget = GetRawUrl() ?? string.Empty;
            // TODO version is slow.
            HttpVersion = GetVersion();
            Scheme = SslStatus != SslStatus.Insecure ? Constants.HttpsScheme : Constants.HttpScheme;
            KnownMethod = VerbId;
            StatusCode = 200;

            var originalPath = GetOriginalPath();

            if (KnownMethod == HttpApiTypes.HTTP_VERB.HttpVerbOPTIONS && string.Equals(RawTarget, "*", StringComparison.Ordinal))
            {
                PathBase = string.Empty;
                Path = string.Empty;
            }
            else
            {
                // Path and pathbase are unescaped by RequestUriBuilder
                // The UsePathBase middleware will modify the pathbase and path correctly
                PathBase = string.Empty;
                Path = originalPath ?? string.Empty;
            }

            var cookedUrl = GetCookedUrl();
            QueryString = cookedUrl.GetQueryString() ?? string.Empty;

            RequestHeaders = new RequestHeaders(this);
            HttpResponseHeaders = new HeaderCollection();
            ResponseHeaders = HttpResponseHeaders;
            // Request headers can be modified by the app, read these first.
            RequestCanHaveBody = CheckRequestCanHaveBody();

            if (_options.ForwardWindowsAuthentication)
            {
                WindowsUser = GetWindowsPrincipal();
                if (_options.AutomaticAuthentication)
                {
                    User = WindowsUser;
                }
            }

            MaxRequestBodySize = _options.MaxRequestBodySize;

            ResetFeatureCollection();

            if (!_server.IsWebSocketAvailable(_requestNativeHandle))
            {
                _currentIHttpUpgradeFeature = null;
            }

            _streams = new Streams(this);

            (RequestBody, ResponseBody) = _streams.Start();

            var pipe = new Pipe(new PipeOptions(
                _memoryPool,
                // The readerScheduler schedules internal non-blocking logic, so there's no reason to dispatch.
                // The writerScheduler is PipeScheduler.ThreadPool by default which is correct because it
                // schedules app code when backpressure is relieved which may block.
                readerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: PauseWriterThreshold,
                resumeWriterThreshold: ResumeWriterTheshold,
                minimumSegmentSize: MinAllocBufferSize));
            _bodyOutput = new OutputProducer(pipe);
        }

        NativeMethods.HttpSetManagedContext(_requestNativeHandle, (IntPtr)_thisHandle);
    }

    private string? GetOriginalPath()
    {
        var rawUrlInBytes = GetRawUrlInBytes();

        // Pre Windows 10 RS2 applicationInitialization request might not have pRawUrl set, fallback to cocked url
        if (rawUrlInBytes.Length == 0)
        {
            return GetCookedUrl().GetAbsPath();
        }

        // ApplicationInitialization request might have trailing \0 character included in the length
        // check and skip it
        if (rawUrlInBytes.Length > 0 && rawUrlInBytes[^1] == 0)
        {
            rawUrlInBytes = rawUrlInBytes[0..^1];
        }

        var originalPath = RequestUriBuilder.DecodeAndUnescapePath(rawUrlInBytes);
        return originalPath;
    }

    public int StatusCode
    {
        get { return _statusCode; }
        set
        {
            if (HasResponseStarted)
            {
                ThrowResponseAlreadyStartedException(nameof(StatusCode));
            }
            _statusCode = (ushort)value;
        }
    }

    public string? ReasonPhrase
    {
        get { return _reasonPhrase; }
        set
        {
            if (HasResponseStarted)
            {
                ThrowResponseAlreadyStartedException(nameof(ReasonPhrase));
            }
            _reasonPhrase = value;
        }
    }

    internal IISHttpServer Server => _server;

    private bool CheckRequestCanHaveBody()
    {
        // Http/1.x requests with bodies require either a Content-Length or Transfer-Encoding header.
        // Note Http.Sys adds the Transfer-Encoding: chunked header to HTTP/2 requests with bodies for back compat.
        // Transfer-Encoding takes priority over Content-Length.
        string transferEncoding = RequestHeaders.TransferEncoding.ToString();
        if (transferEncoding != null && string.Equals("chunked", transferEncoding.Split(',')[^1].Trim(), StringComparison.OrdinalIgnoreCase))
        {
            // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.2
            // A sender MUST NOT send a Content-Length header field in any message
            // that contains a Transfer-Encoding header field.
            // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.3
            // If a message is received with both a Transfer-Encoding and a
            // Content-Length header field, the Transfer-Encoding overrides the
            // Content-Length.  Such a message might indicate an attempt to
            // perform request smuggling (Section 9.5) or response splitting
            // (Section 9.4) and ought to be handled as an error.  A sender MUST
            // remove the received Content-Length field prior to forwarding such
            // a message downstream.
            // We should remove the Content-Length request header in this case, for compatibility
            // reasons, include X-Content-Length so that the original Content-Length is still available.
            if (RequestHeaders.ContentLength.HasValue)
            {
                RequestHeaders.Add("X-Content-Length", RequestHeaders[HeaderNames.ContentLength]);
                RequestHeaders.ContentLength = null;
            }
            return true;
        }

        return RequestHeaders.ContentLength.GetValueOrDefault() > 0;
    }

    private async Task InitializeResponse(bool flushHeaders)
    {
        await FireOnStarting();

        if (_applicationException != null)
        {
            ThrowResponseAbortedException();
        }

        await ProduceStart(flushHeaders);
    }

    private async Task ProduceStart(bool flushHeaders)
    {
        Debug.Assert(_hasResponseStarted == false);

        _hasResponseStarted = true;

        SetResponseHeaders();

        EnsureIOInitialized();
        var canHaveNonEmptyBody = StatusCodeCanHaveBody();
        if (flushHeaders)
        {
            try
            {
                await AsyncIO.FlushAsync(canHaveNonEmptyBody);
            }
            // Client might be disconnected at this point
            // don't leak the exception
            catch (ConnectionResetException)
            {
                AbortIO(clientDisconnect: true);
            }
        }

        if (!canHaveNonEmptyBody)
        {
            _bodyOutput.Complete();
        }
        else
        {
            _writeBodyTask = WriteBody(!flushHeaders);
        }
    }

    private bool StatusCodeCanHaveBody()
    {
        return StatusCode != 204
            && StatusCode != 304;
    }

    private void InitializeRequestIO()
    {
        Debug.Assert(!HasStartedConsumingRequestBody);

        if (RequestHeaders.ContentLength > MaxRequestBodySize)
        {
            IISBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
        }

        HasStartedConsumingRequestBody = true;

        EnsureIOInitialized();

        _bodyInputPipe = new Pipe(new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.ThreadPool, minimumSegmentSize: MinAllocBufferSize));
        _readBodyTask = ReadBody();
    }

    [MemberNotNull(nameof(AsyncIO))]
    private void EnsureIOInitialized()
    {
        // If at this point request was not upgraded just start a normal IO engine
        if (AsyncIO == null)
        {
            AsyncIO = new AsyncIOEngine(this, _requestNativeHandle);
        }
    }

    private void ThrowResponseAbortedException()
    {
        throw new ObjectDisposedException(CoreStrings.UnhandledApplicationException, _applicationException);
    }

    protected Task ProduceEnd()
    {
        if (_requestRejectedException != null || _applicationException != null)
        {
            if (HasResponseStarted)
            {
                // We can no longer change the response, so we simply close the connection.
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
            return ProduceEndAwaited();
        }

        return Task.CompletedTask;
    }

    private void SetErrorResponseHeaders(int statusCode)
    {
        StatusCode = statusCode;
        ReasonPhrase = string.Empty;
        HttpResponseHeaders.Clear();
    }

    private async Task ProduceEndAwaited()
    {
        await ProduceStart(flushHeaders: true);
        await _bodyOutput.FlushAsync(default);
    }

    // Response trailers, reset, and GOAWAY are only on HTTP/2+ and require IIS support
    // that is only available on Win 11/Server 2022 or later.
    private static readonly bool OsSupportsAdvancedHttp2 = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348, 0);

    protected bool AdvancedHttp2FeaturesSupported()
    {
        return OsSupportsAdvancedHttp2 &&
            HttpVersion >= System.Net.HttpVersion.Version20 &&
            NativeMethods.HttpHasResponse4(_requestNativeHandle);
    }

    public unsafe void SetResponseHeaders()
    {
        // Verifies we have sent the statuscode before writing a header
        var reasonPhrase = string.IsNullOrEmpty(ReasonPhrase) ? ReasonPhrases.GetReasonPhrase(StatusCode) : ReasonPhrase;

        // This copies data into the underlying buffer
        NativeMethods.HttpSetResponseStatusCode(_requestNativeHandle, (ushort)StatusCode, reasonPhrase);

        if (AdvancedHttp2FeaturesSupported())
        {
            // Check if connection close is set, if so setting goaway
            if (string.Equals(ConnectionClose, HttpResponseHeaders[HeaderNames.Connection], StringComparison.OrdinalIgnoreCase))
            {
                NativeMethods.HttpSetNeedGoAway(_requestNativeHandle);
            }
        }

        HttpResponseHeaders.IsReadOnly = true;
        foreach (var headerPair in HttpResponseHeaders)
        {
            var headerValues = headerPair.Value;

            if (headerPair.Value.Count == 0)
            {
                continue;
            }

            var knownHeaderIndex = HttpApiTypes.HTTP_RESPONSE_HEADER_ID.IndexOfKnownHeader(headerPair.Key);
            for (var i = 0; i < headerValues.Count; i++)
            {
                var headerValue = headerValues[i];

                if (string.IsNullOrEmpty(headerValue))
                {
                    continue;
                }

                var isFirst = i == 0;
                var headerValueBytes = Encoding.UTF8.GetBytes(headerValue);

                fixed (byte* pHeaderValue = headerValueBytes)
                {
                    if (knownHeaderIndex == -1)
                    {
                        var headerNameBytes = Encoding.UTF8.GetBytes(headerPair.Key);
                        fixed (byte* pHeaderName = headerNameBytes)
                        {
                            NativeMethods.HttpResponseSetUnknownHeader(_requestNativeHandle, pHeaderName, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: isFirst);
                        }
                    }
                    else
                    {
                        NativeMethods.HttpResponseSetKnownHeader(_requestNativeHandle, knownHeaderIndex, pHeaderValue, (ushort)headerValueBytes.Length, fReplace: isFirst);
                    }
                }
            }
        }
    }

    public unsafe void SetResponseTrailers()
    {
        HttpResponseTrailers.IsReadOnly = true;
        foreach (var headerPair in HttpResponseTrailers)
        {
            var headerValues = headerPair.Value;

            if (headerValues.Count == 0)
            {
                continue;
            }

            var headerNameBytes = Encoding.ASCII.GetBytes(headerPair.Key);
            fixed (byte* pHeaderName = headerNameBytes)
            {
                var isFirst = true;
                for (var i = 0; i < headerValues.Count; i++)
                {
                    var headerValue = headerValues[i];
                    if (string.IsNullOrEmpty(headerValue))
                    {
                        continue;
                    }

                    var headerValueBytes = Encoding.UTF8.GetBytes(headerValue);
                    fixed (byte* pHeaderValue = headerValueBytes)
                    {
                        NativeMethods.HttpResponseSetTrailer(_requestNativeHandle, pHeaderName, pHeaderValue, (ushort)headerValueBytes.Length, replace: isFirst);
                    }

                    isFirst = false;
                }
            }
        }
    }

    public abstract Task<bool> ProcessRequestAsync();

    public void OnStarting(Func<object, Task> callback, object state)
    {
        lock (_contextLock)
        {
            if (HasResponseStarted)
            {
                throw new InvalidOperationException("Response already started");
            }

            if (_onStarting == null)
            {
                _onStarting = new Stack<KeyValuePair<Func<object, Task>, object>>();
            }
            _onStarting.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
        }
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
        lock (_contextLock)
        {
            if (_onCompleted == null)
            {
                _onCompleted = new Stack<KeyValuePair<Func<object, Task>, object>>();
            }
            _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
        }
    }

    protected async Task FireOnStarting()
    {
        Stack<KeyValuePair<Func<object, Task>, object>>? onStarting = null;
        lock (_contextLock)
        {
            onStarting = _onStarting;
            _onStarting = null;
        }
        if (onStarting != null)
        {
            try
            {
                foreach (var entry in onStarting)
                {
                    await entry.Key.Invoke(entry.Value);
                }
            }
            catch (Exception ex)
            {
                ReportApplicationError(ex);
            }
        }
    }

    protected async Task FireOnCompleted()
    {
        Stack<KeyValuePair<Func<object, Task>, object>>? onCompleted = null;
        lock (_contextLock)
        {
            onCompleted = _onCompleted;
            _onCompleted = null;
        }
        if (onCompleted != null)
        {
            foreach (var entry in onCompleted)
            {
                try
                {
                    await entry.Key.Invoke(entry.Value);
                }
                catch (Exception ex)
                {
                    Log.ApplicationError(_logger, ((IHttpConnectionFeature)this).ConnectionId, ((IHttpRequestIdentifierFeature)this).TraceIdentifier, ex);
                }
            }
        }
    }

    public void SetBadRequestState(BadHttpRequestException ex)
    {
        Log.ConnectionBadRequest(_logger, ((IHttpConnectionFeature)this).ConnectionId, ex);

        if (!HasResponseStarted)
        {
            SetErrorResponseException(ex);
        }

        _requestRejectedException = ex;
    }

    private void SetErrorResponseException(BadHttpRequestException ex)
    {
        SetErrorResponseHeaders(ex.StatusCode);
    }

    protected void ReportApplicationError(Exception ex)
    {
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

        Log.ApplicationError(_logger, ((IHttpConnectionFeature)this).ConnectionId, ((IHttpRequestIdentifierFeature)this).TraceIdentifier, ex);
    }

    public void PostCompletion(NativeMethods.REQUEST_NOTIFICATION_STATUS requestNotificationStatus)
    {
        NativeMethods.HttpSetCompletionStatus(_requestNativeHandle, requestNotificationStatus);
        NativeMethods.HttpPostCompletion(_requestNativeHandle, 0);
    }

    internal void OnAsyncCompletion(int hr, int bytes)
    {
        AsyncIO!.NotifyCompletion(hr, bytes);
    }

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _thisHandle.Free();
            }

            if (WindowsUser?.Identity is WindowsIdentity wi)
            {
                wi.Dispose();
            }

            // Lock to prevent CancelRequestAbortedToken from attempting to cancel a disposed CTS.
            CancellationTokenSource? localAbortCts = null;

            lock (_abortLock)
            {
                localAbortCts = _abortedCts;
                _abortedCts = null;
            }

            localAbortCts?.Dispose();

            disposedValue = true;
        }
    }

    public override void Dispose()
    {
        Dispose(disposing: true);
    }

    private static void ThrowResponseAlreadyStartedException(string name)
    {
        throw new InvalidOperationException(CoreStrings.FormatParameterReadOnlyAfterResponseStarted(name));
    }

    private WindowsPrincipal? GetWindowsPrincipal()
    {
        NativeMethods.HttpGetAuthenticationInformation(_requestNativeHandle, out var authenticationType, out var token);

        if (token != IntPtr.Zero && authenticationType != null)
        {
            if ((authenticationType.Equals(NtlmString, StringComparison.OrdinalIgnoreCase)
                || authenticationType.Equals(NegotiateString, StringComparison.OrdinalIgnoreCase)
                || authenticationType.Equals(BasicString, StringComparison.OrdinalIgnoreCase)))
            {
                return new WindowsPrincipal(new WindowsIdentity(token, authenticationType));
            }
        }
        return null;
    }

    // Invoked by the thread pool
    public void Execute()
    {
        _ = HandleRequest();
    }

    private async Task HandleRequest()
    {
        bool successfulRequest = false;
        try
        {
            successfulRequest = await ProcessRequestAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpContext)}.{nameof(HandleRequest)}.");
        }
        finally
        {
            // Post completion after completing the request to resume the state machine
            PostCompletion(ConvertRequestCompletionResults(successfulRequest));

            // After disposing a safe handle, Dispose() will not block waiting for the pinvokes to finish.
            // Instead Safehandle will call ReleaseHandle on the pinvoke thread when the pinvokes complete
            // and the reference count goes to zero.

            // What this means is we need to wait until ReleaseHandle is called to finish disposal.
            // This is to make sure it is safe to return back to native.
            // The handle implements IValueTaskSource
            _requestNativeHandle.Dispose();

            await new ValueTask<object?>(_requestNativeHandle, _requestNativeHandle.Version);

            // Dispose the context
            Dispose();
        }
    }

    private static NativeMethods.REQUEST_NOTIFICATION_STATUS ConvertRequestCompletionResults(bool success)
    {
        return success ? NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_CONTINUE
                       : NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
    }
}
