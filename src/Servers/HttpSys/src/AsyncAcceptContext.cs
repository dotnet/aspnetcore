// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed unsafe partial class AsyncAcceptContext : IValueTaskSource<RequestContext>, IDisposable
{
    private static readonly IOCompletionCallback IOCallback = IOWaitCallback;
    private readonly PreAllocatedOverlapped _preallocatedOverlapped;
    private readonly IRequestContextFactory _requestContextFactory;
    private readonly ILogger _logger;
    private int _expectedCompletionCount;

    private NativeOverlapped* _overlapped;

    private readonly bool _logExpectationFailures = AppContext.TryGetSwitch(
        "Microsoft.AspNetCore.Server.HttpSys.LogAcceptExpectationFailure", out var enabled) && enabled;

    // mutable struct; do not make this readonly
    private ManualResetValueTaskSourceCore<RequestContext> _mrvts = new()
    {
        // We want to run continuations on the IO threads
        RunContinuationsAsynchronously = false
    };

    private RequestContext? _requestContext;

    internal AsyncAcceptContext(HttpSysListener server, IRequestContextFactory requestContextFactory, ILogger logger)
    {
        Server = server;
        _requestContextFactory = requestContextFactory;
        _preallocatedOverlapped = new(IOCallback, state: this, pinData: null);
        _logger = logger;
    }

    internal HttpSysListener Server { get; }

    internal ValueTask<RequestContext> AcceptAsync()
    {
        _mrvts.Reset();

        AllocateNativeRequest();

        var statusCode = QueueBeginGetContext();
        if (statusCode != ErrorCodes.ERROR_SUCCESS &&
            statusCode != ErrorCodes.ERROR_IO_PENDING)
        {
            // some other bad error, possible(?) return values are:
            // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
            return ValueTask.FromException<RequestContext>(new HttpSysException((int)statusCode));
        }

        return new ValueTask<RequestContext>(this, _mrvts.Version);
    }

    private void IOCompleted(uint errorCode, uint numBytes, bool managed)
    {
        try
        {
            ObserveCompletion(managed); // expectation tracking
            if (errorCode != ErrorCodes.ERROR_SUCCESS &&
                errorCode != ErrorCodes.ERROR_MORE_DATA)
            {
                // (keep all the error handling in one place)
                throw new HttpSysException((int)errorCode);
            }

            Debug.Assert(_requestContext != null);

            if (errorCode == ErrorCodes.ERROR_SUCCESS)
            {
                var requestContext = _requestContext;
                // It's important that we clear the request context before we set the result
                // we want to reuse the acceptContext object for future accepts.
                _requestContext = null;

                try
                {
                    _mrvts.SetResult(requestContext);
                }
                catch (Exception ex)
                {
                    Log.AcceptSetResultFailed(_logger, ex);
                }
            }
            else
            {
                //  (uint)backingBuffer.Length - AlignmentPadding
                AllocateNativeRequest(numBytes, _requestContext.RequestId);

                // We need to issue a new request, either because auth failed, or because our buffer was too small the first time.
                var statusCode = QueueBeginGetContext();

                if (statusCode != ErrorCodes.ERROR_SUCCESS &&
                    statusCode != ErrorCodes.ERROR_IO_PENDING)
                {
                    // some other bad error, possible(?) return values are:
                    // ERROR_INVALID_HANDLE, ERROR_INSUFFICIENT_BUFFER, ERROR_OPERATION_ABORTED
                    // (keep all the error handling in one place)
                    throw new HttpSysException((int)statusCode);
                }
            }
        }
        catch (Exception exception)
        {
            try
            {
                _mrvts.SetException(exception);
            }
            catch (Exception ex)
            {
                Log.AcceptSetResultFailed(_logger, ex);
            }
        }
    }

    private static unsafe void IOWaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
    {
        var acceptContext = (AsyncAcceptContext)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!;
        acceptContext.IOCompleted(errorCode, numBytes, false);
    }

    private void SetExpectCompletion() // we anticipate a completion *might* occur
    {
        // note this is intentionally a "reset and check" rather than Increment, so that we don't spam
        // the logs forever if a glitch occurs
        var value = Interlocked.Exchange(ref _expectedCompletionCount, 1); // should have been 0
        if (value != 0)
        {
            if (_logExpectationFailures)
            {
                Log.AcceptSetExpectationMismatch(_logger, value);
            }
            Debug.Assert(false, nameof(SetExpectCompletion)); // fail hard in debug
        }
    }
    private void CancelExpectCompletion() // due to error-code etc, we no longer anticipate a completion
    {
        var value = Interlocked.Decrement(ref _expectedCompletionCount); // should have been 1, so now 0
        if (value != 0)
        {
            if (_logExpectationFailures)
            {
                Log.AcceptCancelExpectationMismatch(_logger, value);
            }
            Debug.Assert(false, nameof(CancelExpectCompletion)); // fail hard in debug
        }
    }
    private void ObserveCompletion(bool managed) // a completion was invoked
    {
        var value = Interlocked.Decrement(ref _expectedCompletionCount); // should have been 1, so now 0
        if (value != 0)
        {
            if (_logExpectationFailures)
            {
                Log.AcceptObserveExpectationMismatch(_logger, managed ? "managed" : "unmanaged", value);
            }
            Debug.Assert(false, nameof(ObserveCompletion)); // fail hard in debug
        }
    }

    private uint QueueBeginGetContext()
    {
        uint statusCode;
        bool retry;
        do
        {
            Debug.Assert(_requestContext != null);

            retry = false;
            uint bytesTransferred = 0;
            SetExpectCompletion(); // track this *before*, because of timing vs IOCP (could even be effectively synchronous)
            statusCode = HttpApi.HttpReceiveHttpRequest(
                Server.RequestQueue.Handle,
                _requestContext.RequestId,
                // Small perf impact by not using HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY
                // if the request sends header+body in a single TCP packet
                0u,
                _requestContext.NativeRequest,
                _requestContext.Size,
                &bytesTransferred,
                _overlapped);

            switch (statusCode)
            {
                case (ErrorCodes.ERROR_CONNECTION_INVALID or ErrorCodes.ERROR_INVALID_PARAMETER) when _requestContext.RequestId != 0:
                    // ERROR_CONNECTION_INVALID:
                    // The client reset the connection between the time we got the MORE_DATA error and when we called HttpReceiveHttpRequest
                    // with the new buffer. We can clear the request id and move on to the next request.
                    //
                    // ERROR_INVALID_PARAMETER: Historical check from HttpListener.
                    // https://referencesource.microsoft.com/#System/net/System/Net/_ListenerAsyncResult.cs,137
                    // we might get this if somebody stole our RequestId,
                    // set RequestId to 0 and start all over again with the buffer we just allocated
                    // BUGBUG: how can someone steal our request ID?  seems really bad and in need of fix.
                    CancelExpectCompletion();
                    _requestContext.RequestId = 0;
                    retry = true;
                    break;
                case ErrorCodes.ERROR_MORE_DATA:
                    // the buffer was not big enough to fit the headers, we need
                    // to read the RequestId returned, allocate a new buffer of the required size
                    //  (uint)backingBuffer.Length - AlignmentPadding
                    CancelExpectCompletion(); // we'll "expect" again when we retry
                    AllocateNativeRequest(bytesTransferred);
                    retry = true;
                    break;
                case ErrorCodes.ERROR_SUCCESS:
                    if (HttpSysListener.SkipIOCPCallbackOnSuccess)
                    {
                        // IO operation completed synchronously - callback won't be called to signal completion.
                        IOCompleted(statusCode, bytesTransferred, true); // marks completion
                    }
                    // else: callback fired by IOCP (at some point), which marks completion
                    break;
                case ErrorCodes.ERROR_IO_PENDING:
                    break; // no change to state - callback will occur at some point
                default:
                    // fault code, not expecting an IOCP callback
                    CancelExpectCompletion();
                    break;
            }
        }
        while (retry);
        return statusCode;
    }

    private void AllocateNativeRequest(uint? size = null, ulong requestId = 0)
    {
        _requestContext?.ReleasePins();
        _requestContext?.Dispose();

        var boundHandle = Server.RequestQueue.BoundHandle;
        if (_overlapped != null)
        {
            boundHandle.FreeNativeOverlapped(_overlapped);
        }

        _requestContext = _requestContextFactory.CreateRequestContext(size, requestId);
        _overlapped = boundHandle.AllocateNativeOverlapped(_preallocatedOverlapped);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_requestContext != null)
            {
                _requestContext.ReleasePins();
                _requestContext.Dispose();
                _requestContext = null;
            }

            var boundHandle = Server.RequestQueue.BoundHandle;

            if (_overlapped != null)
            {
                boundHandle.FreeNativeOverlapped(_overlapped);
                _overlapped = null;
            }
        }
    }

    public RequestContext GetResult(short token)
    {
        return _mrvts.GetResult(token);
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _mrvts.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _mrvts.OnCompleted(continuation, state, token, flags);
    }
}
