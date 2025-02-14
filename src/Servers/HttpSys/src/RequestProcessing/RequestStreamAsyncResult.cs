// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed unsafe class RequestStreamAsyncResult : IAsyncResult, IDisposable
{
    private static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(Callback);

    private readonly SafeNativeOverlapped? _overlapped;
    private readonly IntPtr _pinnedBuffer;
    private readonly int _size;
    private readonly uint _dataAlreadyRead;
    private readonly TaskCompletionSource<int> _tcs;
    private readonly RequestStream _requestStream;
    private readonly AsyncCallback? _callback;
    private readonly CancellationTokenRegistration _cancellationRegistration;

    internal RequestStreamAsyncResult(RequestStream requestStream, object? userState, AsyncCallback? callback, byte[] buffer, int offset, int size, uint dataAlreadyRead, CancellationTokenRegistration cancellationRegistration)
    {
        _requestStream = requestStream;
        _tcs = new TaskCompletionSource<int>(userState);
        _callback = callback;
        _dataAlreadyRead = dataAlreadyRead;
        var boundHandle = requestStream.RequestContext.Server.RequestQueue.BoundHandle;
        _overlapped = new SafeNativeOverlapped(boundHandle,
            boundHandle.AllocateNativeOverlapped(IOCallback, this, buffer));
        _pinnedBuffer = (Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
        _size = size;
        _cancellationRegistration = cancellationRegistration;
    }

    internal RequestStream RequestStream
    {
        get { return _requestStream; }
    }

    internal SafeNativeOverlapped? NativeOverlapped
    {
        get { return _overlapped; }
    }

    internal IntPtr PinnedBuffer
    {
        get { return _pinnedBuffer; }
    }

    internal uint DataAlreadyRead
    {
        get { return _dataAlreadyRead; }
    }

    internal Task<int> Task
    {
        get { return _tcs.Task; }
    }

    internal void IOCompleted(uint errorCode, uint numBytes)
    {
        IOCompleted(this, errorCode, numBytes);
    }

    [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Redirecting to callback")]
    private static void IOCompleted(RequestStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
    {
        try
        {
            // Zero-byte reads
            if (errorCode == ErrorCodes.ERROR_MORE_DATA && asyncResult._size == 0)
            {
                // numBytes returns 1 to let us know there's data available. Don't count it against the request body size yet.
                asyncResult.Complete(0, errorCode);
            }
            else if (errorCode != ErrorCodes.ERROR_SUCCESS && errorCode != ErrorCodes.ERROR_HANDLE_EOF)
            {
                asyncResult.Fail(new IOException(string.Empty, new HttpSysException((int)errorCode)));
            }
            else
            {
                // TODO: Verbose log dump data read
                asyncResult.Complete((int)numBytes, errorCode);
            }
        }
        catch (Exception e)
        {
            asyncResult.Fail(new IOException(string.Empty, e));
        }
    }

    private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
    {
        var asyncResult = (RequestStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!;
        IOCompleted(asyncResult, errorCode, numBytes);
    }

    internal void Complete(int read, uint errorCode = ErrorCodes.ERROR_SUCCESS)
    {
        if (_requestStream.TryCheckSizeLimit(read + (int)DataAlreadyRead, out var exception))
        {
            _tcs.TrySetException(exception);
        }
        else if (_tcs.TrySetResult(read + (int)DataAlreadyRead))
        {
            RequestStream.UpdateAfterRead((uint)errorCode, (uint)(read + DataAlreadyRead));
            if (_callback != null)
            {
                try
                {
                    _callback(this);
                }
                catch (Exception)
                {
                    // TODO: Exception handling? This may be an IO callback thread and throwing here could crash the app.
                }
            }
        }
        Dispose();
    }

    internal void Fail(Exception ex)
    {
        // Make sure the Abort state is set before signaling the callback so we can avoid race condtions with user code.
        Dispose();
        _requestStream.Abort();
        if (_tcs.TrySetException(ex) && _callback != null)
        {
            try
            {
                _callback(this);
            }
            catch (Exception)
            {
                // TODO: Exception handling? This may be an IO callback thread and throwing here could crash the app.
                // TODO: Log
            }
        }
    }

    [SuppressMessage("Microsoft.Usage", "CA2216:DisposableTypesShouldDeclareFinalizer", Justification = "The disposable resource referenced does have a finalizer.")]
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _overlapped?.Dispose();
            _cancellationRegistration.Dispose();
        }
    }

    public object? AsyncState
    {
        get { return _tcs.Task.AsyncState; }
    }

    public WaitHandle AsyncWaitHandle
    {
        get { return ((IAsyncResult)_tcs.Task).AsyncWaitHandle; }
    }

    public bool CompletedSynchronously
    {
        get { return ((IAsyncResult)_tcs.Task).CompletedSynchronously; }
    }

    public bool IsCompleted
    {
        get { return _tcs.Task.IsCompleted; }
    }
}
