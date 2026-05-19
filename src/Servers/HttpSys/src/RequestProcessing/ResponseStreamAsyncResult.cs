// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed unsafe partial class ResponseStreamAsyncResult : IAsyncResult, IDisposable
{
    private static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(Callback);

    private readonly SafeNativeOverlapped? _overlapped;
    private readonly HTTP_DATA_CHUNK[]? _dataChunks;
    private readonly FileStream? _fileStream;
    private readonly ResponseBody _responseStream;
    private readonly TaskCompletionSource<object?> _tcs;
    private uint _bytesSent;
    private readonly CancellationToken _cancellationToken;
    private readonly CancellationTokenRegistration _cancellationRegistration;

    internal ResponseStreamAsyncResult(ResponseBody responseStream, CancellationToken cancellationToken)
    {
        _responseStream = responseStream;
        _tcs = new TaskCompletionSource<object?>();

        var cancellationRegistration = default(CancellationTokenRegistration);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = _responseStream.RequestContext.RegisterForCancellation(cancellationToken);
        }
        _cancellationToken = cancellationToken;
        _cancellationRegistration = cancellationRegistration;
    }

    internal ResponseStreamAsyncResult(ResponseBody responseStream, ArraySegment<byte> data, bool chunked,
        CancellationToken cancellationToken)
        : this(responseStream, cancellationToken)
    {
        var boundHandle = _responseStream.RequestContext.Server.RequestQueue.BoundHandle;
        object[] objectsToPin;

        if (data.Count == 0)
        {
            _dataChunks = null;
            _overlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, null));
            return;
        }

        _dataChunks = new HTTP_DATA_CHUNK[1 + (chunked ? 2 : 0)];
        objectsToPin = new object[_dataChunks.Length + 1];
        objectsToPin[0] = _dataChunks;
        var currentChunk = 0;
        var currentPin = 1;

        var chunkHeaderBuffer = new ArraySegment<byte>();
        if (chunked)
        {
            chunkHeaderBuffer = Helpers.GetChunkHeader(data.Count);
            SetDataChunk(_dataChunks, ref currentChunk, objectsToPin, ref currentPin, chunkHeaderBuffer);
        }

        SetDataChunk(_dataChunks, ref currentChunk, objectsToPin, ref currentPin, data);

        if (chunked)
        {
            SetDataChunkWithPinnedData(_dataChunks, ref currentChunk, Helpers.CRLF);
        }

        // This call will pin needed memory
        _overlapped = new SafeNativeOverlapped(boundHandle,
            boundHandle.AllocateNativeOverlapped(IOCallback, this, objectsToPin));

        currentChunk = 0;
        if (chunked)
        {
            _dataChunks[currentChunk].Anonymous.FromMemory.pBuffer = (void*)Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer.Array!, chunkHeaderBuffer.Offset);
            currentChunk++;
        }

        _dataChunks[currentChunk].Anonymous.FromMemory.pBuffer = (void*)Marshal.UnsafeAddrOfPinnedArrayElement(data.Array!, data.Offset);
        currentChunk++;

        if (chunked)
        {
            // No need to update this chunk, CRLF, because it was already pinned.
            // However, we increment the counter to ensure we are accounting for all sections.
            currentChunk++;
        }

        Debug.Assert(currentChunk == _dataChunks.Length);
    }

    internal ResponseStreamAsyncResult(ResponseBody responseStream, FileStream fileStream, long offset,
        long count, bool chunked, CancellationToken cancellationToken)
        : this(responseStream, cancellationToken)
    {
        var boundHandle = responseStream.RequestContext.Server.RequestQueue.BoundHandle;

        _fileStream = fileStream;

        if (count == 0)
        {
            _dataChunks = null;
            _overlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, null));
        }
        else
        {
            _dataChunks = new HTTP_DATA_CHUNK[chunked ? 3 : 1];

            object[] objectsToPin = new object[_dataChunks.Length];
            objectsToPin[_dataChunks.Length - 1] = _dataChunks;

            var chunkHeaderBuffer = new ArraySegment<byte>();
            if (chunked)
            {
                chunkHeaderBuffer = Helpers.GetChunkHeader(count);
                _dataChunks[0].DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                _dataChunks[0].Anonymous.FromMemory.BufferLength = (uint)chunkHeaderBuffer.Count;
                objectsToPin[0] = chunkHeaderBuffer.Array!;

                _dataChunks[1].DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromFileHandle;
                _dataChunks[1].Anonymous.FromFileHandle.ByteRange.StartingOffset = (ulong)offset;
                _dataChunks[1].Anonymous.FromFileHandle.ByteRange.Length = (ulong)count;
                _dataChunks[1].Anonymous.FromFileHandle.FileHandle = (HANDLE)_fileStream.SafeFileHandle.DangerousGetHandle();
                // Nothing to pin for the file handle.

                // No need to pin the CRLF data
                int currentChunk = 2;
                SetDataChunkWithPinnedData(_dataChunks, ref currentChunk, Helpers.CRLF);
                Debug.Assert(currentChunk == _dataChunks.Length);
            }
            else
            {
                _dataChunks[0].DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromFileHandle;
                _dataChunks[0].Anonymous.FromFileHandle.ByteRange.StartingOffset = (ulong)offset;
                _dataChunks[0].Anonymous.FromFileHandle.ByteRange.Length = (ulong)count;
                _dataChunks[0].Anonymous.FromFileHandle.FileHandle = (HANDLE)_fileStream.SafeFileHandle.DangerousGetHandle();
            }

            // This call will pin needed memory
            _overlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, objectsToPin));

            if (chunked)
            {
                // This must be set after pinning with Overlapped.
                _dataChunks[0].Anonymous.FromMemory.pBuffer = (void*)Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer.Array!, chunkHeaderBuffer.Offset);
            }
        }
    }

    private static void SetDataChunk(HTTP_DATA_CHUNK[] chunks, ref int chunkIndex, object[] objectsToPin, ref int pinIndex, ArraySegment<byte> segment)
    {
        objectsToPin[pinIndex] = segment.Array!;
        pinIndex++;
        ref var chunk = ref chunks[chunkIndex++];
        chunk.DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
        // The address is not set until after we pin it with Overlapped
        chunk.Anonymous.FromMemory.BufferLength = (uint)segment.Count;
    }

    private static void SetDataChunkWithPinnedData(HTTP_DATA_CHUNK[] chunks, ref int chunkIndex, ReadOnlySpan<byte> bytes)
    {
        ref var chunk = ref chunks[chunkIndex++];
        chunk.DataChunkType = HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
        fixed (byte* ptr = bytes)
        {
            chunk.Anonymous.FromMemory.pBuffer = ptr;
        }
        chunk.Anonymous.FromMemory.BufferLength = (uint)bytes.Length;
    }

    internal SafeNativeOverlapped? NativeOverlapped
    {
        get { return _overlapped; }
    }

    internal Task Task
    {
        get { return _tcs.Task; }
    }

    internal uint BytesSent
    {
        get { return _bytesSent; }
        set { _bytesSent = value; }
    }

    internal ushort DataChunkCount
    {
        get
        {
            if (_dataChunks == null)
            {
                return 0;
            }
            else
            {
                return (ushort)_dataChunks.Length;
            }
        }
    }

    internal HTTP_DATA_CHUNK* DataChunks
    {
        get
        {
            if (_dataChunks == null)
            {
                return null;
            }
            else
            {
                return (HTTP_DATA_CHUNK*)(Marshal.UnsafeAddrOfPinnedArrayElement(_dataChunks, 0));
            }
        }
    }

    internal bool EndCalled { get; set; }

    internal void IOCompleted(uint errorCode)
    {
        IOCompleted(this, errorCode);
    }

    private static void IOCompleted(ResponseStreamAsyncResult asyncResult, uint errorCode)
    {
        var logger = asyncResult._responseStream.RequestContext.Logger;
        try
        {
            if (errorCode != ErrorCodes.ERROR_SUCCESS && errorCode != ErrorCodes.ERROR_HANDLE_EOF)
            {
                if (asyncResult._cancellationToken.IsCancellationRequested)
                {
                    Log.WriteCancelled(logger, errorCode);
                    asyncResult.Cancel(asyncResult._responseStream.ThrowWriteExceptions);
                }
                else if (asyncResult._responseStream.ThrowWriteExceptions)
                {
                    var exception = new IOException(string.Empty, new HttpSysException((int)errorCode));
                    Log.WriteError(logger, exception);
                    asyncResult.Fail(exception);
                }
                else
                {
                    Log.WriteErrorIgnored(logger, errorCode);
                    asyncResult.FailSilently();
                }
            }
            else
            {
                if (asyncResult._dataChunks == null)
                {
                    // TODO: Verbose log data written
                }
                else
                {
                    // TODO: Verbose log
                    // for (int i = 0; i < asyncResult._dataChunks.Length; i++)
                    // {
                    // Logging.Dump(Logging.HttpListener, asyncResult, "Callback", (IntPtr)asyncResult._dataChunks[0].fromMemory.pBuffer, (int)asyncResult._dataChunks[0].fromMemory.BufferLength);
                    // }
                }
                asyncResult.Complete();
            }
        }
        catch (Exception e)
        {
            Log.WriteError(logger, e);
            asyncResult.Fail(e);
        }
    }

    private static unsafe void Callback(uint errorCode, uint _, NativeOverlapped* nativeOverlapped)
    {
        var asyncResult = (ResponseStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped)!;
        IOCompleted(asyncResult, errorCode);
    }

    internal void Complete()
    {
        Dispose();
        _tcs.TrySetResult(null);
    }

    internal void FailSilently()
    {
        Dispose();
        // Abort the request but do not close the stream, let future writes complete silently
        _responseStream.Abort(dispose: false);
        _tcs.TrySetResult(null);
    }

    internal void Cancel(bool dispose)
    {
        Dispose();
        _responseStream.Abort(dispose);
        _tcs.TrySetCanceled();
    }

    internal void Fail(Exception ex)
    {
        Dispose();
        _responseStream.Abort();
        _tcs.TrySetException(ex);
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

    public void Dispose()
    {
        _overlapped?.Dispose();
        _fileStream?.Dispose();
        _cancellationRegistration.Dispose();
    }
}
