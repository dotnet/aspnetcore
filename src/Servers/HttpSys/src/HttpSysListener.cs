// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// An HTTP server wrapping the Http.Sys APIs that accepts requests.
/// </summary>
internal sealed partial class HttpSysListener : IDisposable
{
    // Win8# 559317 fixed a bug in Http.sys's HttpReceiveClientCertificate method.
    // Without this fix IOCP callbacks were not being called although ERROR_IO_PENDING was
    // returned from HttpReceiveClientCertificate when using the
    // FileCompletionNotificationModes.SkipCompletionPortOnSuccess flag.
    // This bug was only hit when the buffer passed into HttpReceiveClientCertificate
    // (1500 bytes initially) is too small for the certificate.
    // Due to this bug in downlevel operating systems the FileCompletionNotificationModes.SkipCompletionPortOnSuccess
    // flag is only used on Win8 and later.
    internal static readonly bool SkipIOCPCallbackOnSuccess = ComNetOS.IsWin8orLater;

    // Mitigate potential DOS attacks by limiting the number of unknown headers we accept.  Numerous header names
    // with hash collisions will cause the server to consume excess CPU.  1000 headers limits CPU time to under
    // 0.5 seconds per request.  Respond with a 400 Bad Request.
    private const int UnknownHeaderLimit = 1000;

    internal MemoryPool<byte> MemoryPool { get; } = PinnedBlockMemoryPoolFactory.Create();

    private volatile State _state; // m_State is set only within lock blocks, but often read outside locks.

    private readonly ServerSession _serverSession;
    private readonly UrlGroup _urlGroup;
    private readonly RequestQueue _requestQueue;
    private readonly DisconnectListener _disconnectListener;

    private readonly object _internalLock;

    public HttpSysListener(HttpSysOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (!HttpApi.Supported)
        {
            throw new PlatformNotSupportedException();
        }

        Options = options;

        Logger = loggerFactory.CreateLogger<HttpSysListener>();

        _state = State.Stopped;
        _internalLock = new object();

        // V2 initialization sequence:
        // 1. Create server session
        // 2. Create url group
        // 3. Create request queue
        // 4. Add urls to url group - Done in Start()
        // 5. Attach request queue to url group - Done in Start()

        try
        {
            _serverSession = new ServerSession();

            _requestQueue = new RequestQueue(options.RequestQueueName, options.RequestQueueMode, Logger);

            _urlGroup = new UrlGroup(_serverSession, _requestQueue, Logger);

            _disconnectListener = new DisconnectListener(_requestQueue, Logger);
        }
        catch (Exception exception)
        {
            // If Url group or request queue creation failed, close server session before throwing.
            _requestQueue?.Dispose();
            _urlGroup?.Dispose();
            _serverSession?.Dispose();
            Log.HttpSysListenerCtorError(Logger, exception);
            throw;
        }
    }

    internal enum State
    {
        Stopped,
        Started,
        Disposed,
    }

    internal ILogger Logger { get; private set; }

    internal UrlGroup UrlGroup
    {
        get { return _urlGroup; }
    }

    internal RequestQueue RequestQueue
    {
        get { return _requestQueue; }
    }

    internal DisconnectListener DisconnectListener
    {
        get { return _disconnectListener; }
    }

    public HttpSysOptions Options { get; }

    public bool IsListening
    {
        get { return _state == State.Started; }
    }

    /// <summary>
    /// Start accepting incoming requests.
    /// </summary>
    public void Start()
    {
        CheckDisposed();

        Log.ListenerStarting(Logger);

        // Make sure there are no race conditions between Start/Stop/Abort/Close/Dispose.
        // Start needs to setup all resources. Abort/Stop must not interfere while Start is
        // allocating those resources.
        lock (_internalLock)
        {
            try
            {
                CheckDisposed();
                if (_state == State.Started)
                {
                    return;
                }

                // Always configure the UrlGroup if the intent was to create, only configure the queue if we actually created it
                if (Options.RequestQueueMode == RequestQueueMode.Create || Options.RequestQueueMode == RequestQueueMode.CreateOrAttach)
                {
                    Options.Apply(UrlGroup, _requestQueue.Created ? RequestQueue : null);

                    UrlGroup.AttachToQueue();

                    // All resources are set up correctly. Now add all prefixes.
                    try
                    {
                        Options.UrlPrefixes.RegisterAllPrefixes(UrlGroup);
                    }
                    catch (HttpSysException)
                    {
                        // If an error occurred while adding prefixes, free all resources allocated by previous steps.
                        UrlGroup.DetachFromQueue();
                        throw;
                    }
                }

                _state = State.Started;
            }
            catch (Exception exception)
            {
                // Make sure the HttpListener instance can't be used if Start() failed.
                _state = State.Disposed;
                DisposeInternal();
                Log.ListenerStartError(Logger, exception);
                throw;
            }
        }
    }

    private void Stop()
    {
        try
        {
            lock (_internalLock)
            {
                CheckDisposed();
                if (_state == State.Stopped)
                {
                    return;
                }

                Log.ListenerStopping(Logger);

                // If this instance registered URL prefixes then remove them before shutting down.
                if (Options.RequestQueueMode == RequestQueueMode.Create || Options.RequestQueueMode == RequestQueueMode.CreateOrAttach)
                {
                    Options.UrlPrefixes.UnregisterAllPrefixes();
                    UrlGroup.DetachFromQueue();
                }

                _state = State.Stopped;

            }
        }
        catch (Exception exception)
        {
            Log.ListenerStopError(Logger, exception);
            throw;
        }
    }

    /// <summary>
    /// Stop the server and clean up.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        lock (_internalLock)
        {
            try
            {
                if (_state == State.Disposed)
                {
                    return;
                }
                Log.ListenerDisposing(Logger);

                Stop();
                DisposeInternal();
            }
            catch (Exception exception)
            {
                Log.ListenerDisposeError(Logger, exception);
                throw;
            }
            finally
            {
                _state = State.Disposed;
            }
        }
    }

    private void DisposeInternal()
    {
        // V2 stopping sequence:
        // 1. Detach request queue from url group - Done in Stop()/Abort()
        // 2. Remove urls from url group - Done in Stop()
        // 3. Close request queue - Done in Stop()/Abort()
        // 4. Close Url group.
        // 5. Close server session.

        _requestQueue.Dispose();

        _urlGroup.Dispose();

        Debug.Assert(_serverSession != null, "ServerSessionHandle is null in CloseV2Config");
        Debug.Assert(!_serverSession.Id.IsInvalid, "ServerSessionHandle is invalid in CloseV2Config");

        _serverSession.Dispose();
    }

    /// <summary>
    /// Accept a request from the incoming request queue.
    /// </summary>
    internal ValueTask<RequestContext> AcceptAsync(AsyncAcceptContext acceptContext)
    {
        CheckDisposed();
        Debug.Assert(_state != State.Stopped, "Listener has been stopped.");

        return acceptContext.AcceptAsync();
    }

    internal bool ValidateRequest(NativeRequestContext requestMemory)
    {
        try
        {
            // Block potential DOS attacks
            if (requestMemory.UnknownHeaderCount > UnknownHeaderLimit)
            {
                SendError(requestMemory.RequestId, StatusCodes.Status400BadRequest, authChallenges: null);
                return false;
            }

            if (!Options.Authentication.AllowAnonymous && !requestMemory.CheckAuthenticated())
            {
                SendError(requestMemory.RequestId, StatusCodes.Status401Unauthorized,
                    AuthenticationManager.GenerateChallenges(Options.Authentication.Schemes));
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.RequestValidationFailed(Logger, ex, requestMemory.RequestId);
            return false;
        }

        return true;
    }

    internal unsafe void SendError(ulong requestId, int httpStatusCode, IList<string>? authChallenges = null)
    {
        var httpResponse = new HTTP_RESPONSE_V2();
        httpResponse.Base.Version = new()
        {
            MajorVersion = 1,
            MinorVersion = 1
        };

        using UnmanagedBufferAllocator allocator = new();

        byte* bytes;
        int bytesLength;

        // Copied from the multi-value headers section of SerializeHeaders
        if (authChallenges != null && authChallenges.Count > 0)
        {
            var knownHeaderInfo = allocator.AllocAsPointer<HTTP_RESPONSE_INFO>(1);
            httpResponse.pResponseInfo = knownHeaderInfo;

            knownHeaderInfo[httpResponse.ResponseInfoCount].Type = HTTP_RESPONSE_INFO_TYPE.HttpResponseInfoTypeMultipleKnownHeaders;
            knownHeaderInfo[httpResponse.ResponseInfoCount].Length =
                (uint)sizeof(HTTP_MULTIPLE_KNOWN_HEADERS);

            var header = allocator.AllocAsPointer<HTTP_MULTIPLE_KNOWN_HEADERS>(1);

            header->HeaderId = HTTP_HEADER_ID.HttpHeaderWwwAuthenticate;
            header->Flags = PInvoke.HTTP_RESPONSE_INFO_FLAGS_PRESERVE_ORDER; // The docs say this is for www-auth only.
            header->KnownHeaderCount = 0;

            var nativeHeaderValues = allocator.AllocAsPointer<HTTP_KNOWN_HEADER>(authChallenges.Count);
            header->KnownHeaders = nativeHeaderValues;

            for (var headerValueIndex = 0; headerValueIndex < authChallenges.Count; headerValueIndex++)
            {
                // Add Value
                var headerValue = authChallenges[headerValueIndex];
                bytes = allocator.GetHeaderEncodedBytes(headerValue, out bytesLength);
                nativeHeaderValues[header->KnownHeaderCount].RawValueLength = checked((ushort)bytesLength);
                nativeHeaderValues[header->KnownHeaderCount].pRawValue = (PCSTR)bytes;
                header->KnownHeaderCount++;
            }

            knownHeaderInfo[0].pInfo = header;

            httpResponse.ResponseInfoCount = 1;
        }

        httpResponse.Base.StatusCode = checked((ushort)httpStatusCode);
        var statusDescription = HttpReasonPhrase.Get(httpStatusCode) ?? string.Empty;
        uint dataWritten = 0;
        uint statusCode;

        bytes = allocator.GetHeaderEncodedBytes(statusDescription, out bytesLength);
        httpResponse.Base.pReason = (PCSTR)bytes;
        httpResponse.Base.ReasonLength = checked((ushort)bytesLength);

        const int contentLengthLength = 1;
        var pContentLength = allocator.AllocAsPointer<byte>(contentLengthLength + 1);
        pContentLength[0] = (byte)'0';
        pContentLength[1] = 0; // null terminator

        var knownHeaders = httpResponse.Base.Headers.KnownHeaders.AsSpan();
        knownHeaders[(int)HttpSysResponseHeader.ContentLength].pRawValue = (PCSTR)pContentLength;
        knownHeaders[(int)HttpSysResponseHeader.ContentLength].RawValueLength = contentLengthLength;
        httpResponse.Base.Headers.UnknownHeaderCount = 0;

        statusCode = PInvoke.HttpSendHttpResponse(
            _requestQueue.Handle,
            requestId,
            0,
            httpResponse,
            null,
            &dataWritten,
            null,
            null);
        if (statusCode != ErrorCodes.ERROR_SUCCESS)
        {
            // if we fail to send a 401 something's seriously wrong, abort the request
            PInvoke.HttpCancelHttpRequest(_requestQueue.Handle, requestId, default);
        }
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_state == State.Disposed, this);
    }
}
