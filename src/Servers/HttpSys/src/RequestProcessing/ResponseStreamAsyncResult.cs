// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal unsafe class ResponseStreamAsyncResult : IAsyncResult, IDisposable
    {
        private static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(Callback);

        private SafeNativeOverlapped _overlapped;
        private HttpApiTypes.HTTP_DATA_CHUNK[] _dataChunks;
        private FileStream _fileStream;
        private ResponseBody _responseStream;
        private TaskCompletionSource<object> _tcs;
        private uint _bytesSent;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationRegistration;

        internal ResponseStreamAsyncResult(ResponseBody responseStream, CancellationToken cancellationToken)
        {
            _responseStream = responseStream;
            _tcs = new TaskCompletionSource<object>();

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

            _dataChunks = new HttpApiTypes.HTTP_DATA_CHUNK[1 + (chunked ? 2 : 0)];
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
                SetDataChunk(_dataChunks, ref currentChunk, objectsToPin, ref currentPin, new ArraySegment<byte>(Helpers.CRLF));
            }

            // This call will pin needed memory
            _overlapped = new SafeNativeOverlapped(boundHandle,
                boundHandle.AllocateNativeOverlapped(IOCallback, this, objectsToPin));

            currentChunk = 0;
            if (chunked)
            {
                _dataChunks[currentChunk].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer.Array, chunkHeaderBuffer.Offset);
                currentChunk++;
            }

            _dataChunks[currentChunk].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(data.Array, data.Offset);
            currentChunk++;

            if (chunked)
            {
                _dataChunks[currentChunk].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(Helpers.CRLF, 0);
                currentChunk++;
            }
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
                _dataChunks = new HttpApiTypes.HTTP_DATA_CHUNK[chunked ? 3 : 1];

                object[] objectsToPin = new object[_dataChunks.Length];
                objectsToPin[_dataChunks.Length - 1] = _dataChunks;

                var chunkHeaderBuffer = new ArraySegment<byte>();
                if (chunked)
                {
                    chunkHeaderBuffer = Helpers.GetChunkHeader(count);
                    _dataChunks[0].DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[0].fromMemory.BufferLength = (uint)chunkHeaderBuffer.Count;
                    objectsToPin[0] = chunkHeaderBuffer.Array;

                    _dataChunks[1].DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromFileHandle;
                    _dataChunks[1].fromFile.offset = (ulong)offset;
                    _dataChunks[1].fromFile.count = (ulong)count;
                    _dataChunks[1].fromFile.fileHandle = _fileStream.SafeFileHandle.DangerousGetHandle();
                    // Nothing to pin for the file handle.

                    _dataChunks[2].DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[2].fromMemory.BufferLength = (uint)Helpers.CRLF.Length;
                    objectsToPin[1] = Helpers.CRLF;
                }
                else
                {
                    _dataChunks[0].DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromFileHandle;
                    _dataChunks[0].fromFile.offset = (ulong)offset;
                    _dataChunks[0].fromFile.count = (ulong)count;
                    _dataChunks[0].fromFile.fileHandle = _fileStream.SafeFileHandle.DangerousGetHandle();
                }

                // This call will pin needed memory
                _overlapped = new SafeNativeOverlapped(boundHandle,
                    boundHandle.AllocateNativeOverlapped(IOCallback, this, objectsToPin));

                if (chunked)
                {
                    // These must be set after pinning with Overlapped.
                    _dataChunks[0].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer.Array, chunkHeaderBuffer.Offset);
                    _dataChunks[2].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(Helpers.CRLF, 0);
                }
            }
        }

        private static void SetDataChunk(HttpApiTypes.HTTP_DATA_CHUNK[] chunks, ref int chunkIndex, object[] objectsToPin, ref int pinIndex, ArraySegment<byte> segment)
        {
            objectsToPin[pinIndex] = segment.Array;
            pinIndex++;
            chunks[chunkIndex].DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
            // The address is not set until after we pin it with Overlapped
            chunks[chunkIndex].fromMemory.BufferLength = (uint)segment.Count;
            chunkIndex++;
        }

        internal SafeNativeOverlapped NativeOverlapped
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

        internal HttpApiTypes.HTTP_DATA_CHUNK* DataChunks
        {
            get
            {
                if (_dataChunks == null)
                {
                    return null;
                }
                else
                {
                    return (HttpApiTypes.HTTP_DATA_CHUNK*)(Marshal.UnsafeAddrOfPinnedArrayElement(_dataChunks, 0));
                }
            }
        }

        internal bool EndCalled { get; set; }

        internal void IOCompleted(uint errorCode)
        {
            IOCompleted(this, errorCode, BytesSent);
        }

        internal void IOCompleted(uint errorCode, uint numBytes)
        {
            IOCompleted(this, errorCode, numBytes);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Redirecting to callback")]
        private static void IOCompleted(ResponseStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
        {
            var logger = asyncResult._responseStream.RequestContext.Logger;
            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
                {
                    if (asyncResult._cancellationToken.IsCancellationRequested)
                    {
                        logger.LogDebug(LoggerEventIds.WriteCancelled,$"FlushAsync.IOCompleted; Write cancelled with error code: {errorCode}");
                        asyncResult.Cancel(asyncResult._responseStream.ThrowWriteExceptions);
                    }
                    else if (asyncResult._responseStream.ThrowWriteExceptions)
                    {
                        var exception = new IOException(string.Empty, new HttpSysException((int)errorCode));
                        logger.LogError(LoggerEventIds.WriteError, exception, "FlushAsync.IOCompleted");
                        asyncResult.Fail(exception);
                    }
                    else
                    {
                        logger.LogDebug(LoggerEventIds.WriteErrorIgnored, $"FlushAsync.IOCompleted; Ignored write exception: {errorCode}");
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
                logger.LogError(LoggerEventIds.WriteError, e, "FlushAsync.IOCompleted");
                asyncResult.Fail(e);
            }
        }

        private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            var asyncResult = (ResponseStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
            IOCompleted(asyncResult, errorCode, numBytes);
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

        public object AsyncState
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
            if (_overlapped != null)
            {
                _overlapped.Dispose();
            }
            if (_fileStream != null)
            {
                _fileStream.Dispose();
            }
            _cancellationRegistration.Dispose();
        }
    }
}
