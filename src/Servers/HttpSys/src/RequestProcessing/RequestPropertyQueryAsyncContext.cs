// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading.Tasks.Sources;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Drives a single asynchronous HttpQueryRequestProperty call. Most properties complete synchronously
/// (per the Win32 docs); when they do, <see cref="StartAsync"/> returns a synchronously-completed
/// <see cref="ValueTask{T}"/> with no IOCP roundtrip and no <see cref="ManualResetValueTaskSourceCore{T}"/> usage.
/// When the OS returns ERROR_IO_PENDING, completion is signalled via the IOCP callback and the task
/// completes asynchronously.
/// </summary>
internal sealed unsafe class RequestPropertyQueryAsyncContext : IValueTaskSource<HttpSysRequestPropertyResult>
{
    private static readonly IOCompletionCallback IOCallback = IOWaitCallback;

    private readonly RequestContext _requestContext;
    private NativeOverlapped* _overlapped;
    private byte[]? _rentedQualifier;
    private MemoryHandle _qualifierHandle;
    private MemoryHandle _outputHandle;
    private CancellationTokenRegistration _cancellationRegistration;

    // mutable struct; do not make this readonly
    private ManualResetValueTaskSourceCore<HttpSysRequestPropertyResult> _mrvts = new()
    {
        // Run continuations on the IO thread (matches AsyncAcceptContext).
        RunContinuationsAsynchronously = false,
    };

    public RequestPropertyQueryAsyncContext(RequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    internal ValueTask<HttpSysRequestPropertyResult> StartAsync(
        HTTP_REQUEST_PROPERTY propertyId,
        ReadOnlySpan<byte> qualifier,
        Memory<byte> output,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<HttpSysRequestPropertyResult>(cancellationToken);
        }

        // HttpQueryRequestProperty requires NULL pointers for empty qualifier/output (it returns
        // ERROR_INVALID_PARAMETER for any other pointer value when the size is 0). Skipping the
        // copy/pin for empty buffers also avoids any allocation on the size-probe path.
        var qualifierLength = qualifier.Length;
        try
        {
            if (qualifierLength > 0)
            {
                // The qualifier is "[in]" data; on the async path HTTP.SYS may retain the pointer
                // until the I/O completes, so we must copy the caller's span into a buffer we can
                // keep alive across the await. ArrayPool keeps this allocation-free in steady state.
                _rentedQualifier = ArrayPool<byte>.Shared.Rent(qualifierLength);
                qualifier.CopyTo(_rentedQualifier);
                _qualifierHandle = _rentedQualifier.AsMemory(0, qualifierLength).Pin();
            }

            if (output.Length > 0)
            {
                _outputHandle = output.Pin();
            }

            var boundHandle = _requestContext.Server.RequestQueue.BoundHandle;
            _overlapped = boundHandle.AllocateNativeOverlapped(IOCallback, state: this, pinData: null);

            var requestId = _requestContext.PinsReleased
                ? _requestContext.Request.RequestId
                : _requestContext.RequestId;

            // Stack-allocated; only read on the synchronous return path. On the async (ERROR_IO_PENDING)
            // path the IOCP callback receives the byte count separately in `numBytes`, so we never look
            // at this local after the call returns ERROR_IO_PENDING.
            uint bytesReturnedSync = 0;

            var statusCode = HttpApi.HttpGetRequestProperty(
                requestQueueHandle: _requestContext.Server.RequestQueue.Handle,
                requestId: requestId,
                propertyId: propertyId,
                qualifier: qualifierLength > 0 ? _qualifierHandle.Pointer : null,
                qualifierSize: (uint)qualifierLength,
                output: output.Length > 0 ? _outputHandle.Pointer : null,
                outputSize: (uint)output.Length,
                bytesReturned: (IntPtr)(&bytesReturnedSync),
                overlapped: (IntPtr)_overlapped);

            switch (statusCode)
            {
                case ErrorCodes.ERROR_SUCCESS when HttpSysListener.SkipIOCPCallbackOnSuccess:
                    // Sync success fast-path: no IOCP callback will fire on Win8+.
                    Cleanup();
                    return ValueTask.FromResult(new HttpSysRequestPropertyResult
                    {
                        Succeeded = true,
                        BytesReturned = checked((int)bytesReturnedSync),
                    });

                case ErrorCodes.ERROR_INSUFFICIENT_BUFFER:
                case ErrorCodes.ERROR_MORE_DATA:
                    // "Buffer too small" always returns synchronously; no IOCP callback will fire.
                    Cleanup();
                    return ValueTask.FromResult(new HttpSysRequestPropertyResult
                    {
                        Succeeded = false,
                        BytesReturned = checked((int)bytesReturnedSync),
                    });

                case ErrorCodes.ERROR_SUCCESS:
                    // Pre-Win8 fallback: IOCP callback will fire even on sync success. Fall through
                    // to the async path. (ERROR_SUCCESS without SkipIOCPCallbackOnSuccess.)
                case ErrorCodes.ERROR_IO_PENDING:
                    if (cancellationToken.CanBeCanceled)
                    {
                        // Pre-cancellation only — the OS doesn't expose a way to cancel an in-flight
                        // HttpQueryRequestProperty without tearing down the entire HTTP request.
                        // If the OS itself aborts the operation we'll surface ERROR_OPERATION_ABORTED
                        // from the IOCP callback and translate to OperationCanceledException.
                        _cancellationRegistration = cancellationToken.UnsafeRegister(static (_, _) => { }, this);
                    }
                    return new ValueTask<HttpSysRequestPropertyResult>(this, _mrvts.Version);

                default:
                    Cleanup();
                    return ValueTask.FromException<HttpSysRequestPropertyResult>(new HttpSysException((int)statusCode));
            }
        }
        catch
        {
            Cleanup();
            throw;
        }
    }

    private static void IOWaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
    {
        var ctx = (RequestPropertyQueryAsyncContext)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!;
        ctx.IOCompleted(errorCode, numBytes);
    }

    private void IOCompleted(uint errorCode, uint numBytes)
    {
        Cleanup();
        try
        {
            switch (errorCode)
            {
                case ErrorCodes.ERROR_SUCCESS:
                    _mrvts.SetResult(new HttpSysRequestPropertyResult
                    {
                        Succeeded = true,
                        BytesReturned = checked((int)numBytes),
                    });
                    break;

                case ErrorCodes.ERROR_INSUFFICIENT_BUFFER:
                case ErrorCodes.ERROR_MORE_DATA:
                    _mrvts.SetResult(new HttpSysRequestPropertyResult
                    {
                        Succeeded = false,
                        BytesReturned = checked((int)numBytes),
                    });
                    break;

                case ErrorCodes.ERROR_OPERATION_ABORTED:
                    _mrvts.SetException(new OperationCanceledException());
                    break;

                default:
                    _mrvts.SetException(new HttpSysException((int)errorCode));
                    break;
            }
        }
        catch (Exception ex)
        {
            _mrvts.SetException(ex);
        }
    }

    private void Cleanup()
    {
        if (_overlapped != null)
        {
            _requestContext.Server.RequestQueue.BoundHandle.FreeNativeOverlapped(_overlapped);
            _overlapped = null;
        }
        _qualifierHandle.Dispose();
        _outputHandle.Dispose();
        if (_rentedQualifier is not null)
        {
            ArrayPool<byte>.Shared.Return(_rentedQualifier);
            _rentedQualifier = null;
        }
        _cancellationRegistration.Dispose();
    }

    public HttpSysRequestPropertyResult GetResult(short token) => _mrvts.GetResult(token);

    public ValueTaskSourceStatus GetStatus(short token) => _mrvts.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _mrvts.OnCompleted(continuation, state, token, flags);
}
