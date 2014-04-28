// -----------------------------------------------------------------------
// <copyright file="ResponseStreamAsyncResult.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.Server
{
    internal unsafe class ResponseStreamAsyncResult : IAsyncResult, IDisposable
    {
        private static readonly byte[] CRLF = new byte[] { (byte)'\r', (byte)'\n' };
        private static readonly IOCompletionCallback IOCallback = new IOCompletionCallback(Callback);

        private SafeNativeOverlapped _overlapped;
        private UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK[] _dataChunks;
        private bool _sentHeaders;
        private FileStream _fileStream;
        private ResponseStream _responseStream;
        private TaskCompletionSource<object> _tcs;
        private AsyncCallback _callback;
        private uint _bytesSent;

        internal ResponseStreamAsyncResult(ResponseStream responseStream, object userState, AsyncCallback callback)
        {
            _responseStream = responseStream;
            _tcs = new TaskCompletionSource<object>(userState);
            _callback = callback;
        }

        internal ResponseStreamAsyncResult(ResponseStream responseStream, object userState, AsyncCallback callback,
            byte[] buffer, int offset, int size, bool chunked, bool sentHeaders)
            : this(responseStream, userState, callback)
        {
            _sentHeaders = sentHeaders;
            Overlapped overlapped = new Overlapped();
            overlapped.AsyncResult = this;

            if (size == 0)
            {
                _dataChunks = null;
                _overlapped = new SafeNativeOverlapped(overlapped.Pack(IOCallback, null));
            }
            else
            {
                _dataChunks = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK[chunked ? 3 : 1];

                object[] objectsToPin = new object[1 + _dataChunks.Length];
                objectsToPin[_dataChunks.Length] = _dataChunks;

                int chunkHeaderOffset = 0;
                byte[] chunkHeaderBuffer = null;
                if (chunked)
                {
                    chunkHeaderBuffer = GetChunkHeader(size, out chunkHeaderOffset);

                    _dataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[0].fromMemory.BufferLength = (uint)(chunkHeaderBuffer.Length - chunkHeaderOffset);

                    objectsToPin[0] = chunkHeaderBuffer;

                    _dataChunks[1] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[1].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[1].fromMemory.BufferLength = (uint)size;

                    objectsToPin[1] = buffer;

                    _dataChunks[2] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[2].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[2].fromMemory.BufferLength = (uint)CRLF.Length;

                    objectsToPin[2] = CRLF;
                }
                else
                {
                    _dataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[0].fromMemory.BufferLength = (uint)size;

                    objectsToPin[0] = buffer;
                }

                // This call will pin needed memory
                _overlapped = new SafeNativeOverlapped(overlapped.Pack(IOCallback, objectsToPin));

                if (chunked)
                {
                    _dataChunks[0].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer, chunkHeaderOffset);
                    _dataChunks[1].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
                    _dataChunks[2].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(CRLF, 0);
                }
                else
                {
                    _dataChunks[0].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
                }
            }
        }

        internal ResponseStreamAsyncResult(ResponseStream responseStream, object userState, AsyncCallback callback,
            string fileName, long offset, long? size, bool chunked, bool sentHeaders)
            : this(responseStream, userState, callback)
        {
            _sentHeaders = sentHeaders;
            Overlapped overlapped = new Overlapped();
            overlapped.AsyncResult = this;

            int bufferSize = 1024 * 64; // TODO: Validate buffer size choice.
#if NET45
            // It's too expensive to validate anything before opening the file. Open the file and then check the lengths.
            _fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan); // Extremely expensive.
#else
            _fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize /*, useAsync: true*/); // Extremely expensive.
#endif
            long length = _fileStream.Length; // Expensive
            if (offset < 0 || offset > length)
            {
                _fileStream.Dispose();
                throw new ArgumentOutOfRangeException("offset", offset, string.Empty);
            }
            if (size.HasValue && (size < 0 || size > length - offset))
            {
                _fileStream.Dispose();
                throw new ArgumentOutOfRangeException("size", size, string.Empty);
            }

            if (size == 0 || (!size.HasValue && _fileStream.Length == 0))
            {
                _dataChunks = null;
                _overlapped = new SafeNativeOverlapped(overlapped.Pack(IOCallback, null));
            }
            else
            {
                _dataChunks = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK[chunked ? 3 : 1];

                object[] objectsToPin = new object[_dataChunks.Length];
                objectsToPin[_dataChunks.Length - 1] = _dataChunks;

                int chunkHeaderOffset = 0;
                byte[] chunkHeaderBuffer = null;
                if (chunked)
                {
                    chunkHeaderBuffer = GetChunkHeader((int)(size ?? _fileStream.Length - offset), out chunkHeaderOffset);

                    _dataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[0].fromMemory.BufferLength = (uint)(chunkHeaderBuffer.Length - chunkHeaderOffset);

                    objectsToPin[0] = chunkHeaderBuffer;

                    _dataChunks[1] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[1].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromFileHandle;
                    _dataChunks[1].fromFile.offset = (ulong)offset;
                    _dataChunks[1].fromFile.count = (ulong)(size ?? -1);
                    _dataChunks[1].fromFile.fileHandle = _fileStream.SafeFileHandle.DangerousGetHandle();
                    // Nothing to pin for the file handle.

                    _dataChunks[2] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[2].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
                    _dataChunks[2].fromMemory.BufferLength = (uint)CRLF.Length;

                    objectsToPin[1] = CRLF;
                }
                else
                {
                    _dataChunks[0] = new UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK();
                    _dataChunks[0].DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromFileHandle;
                    _dataChunks[0].fromFile.offset = (ulong)offset;
                    _dataChunks[0].fromFile.count = (ulong)(size ?? -1);
                    _dataChunks[0].fromFile.fileHandle = _fileStream.SafeFileHandle.DangerousGetHandle();
                }

                // This call will pin needed memory
                _overlapped = new SafeNativeOverlapped(overlapped.Pack(IOCallback, objectsToPin));

                if (chunked)
                {
                    _dataChunks[0].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(chunkHeaderBuffer, chunkHeaderOffset);
                    _dataChunks[2].fromMemory.pBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(CRLF, 0);
                }
            }
        }

        internal ResponseStream ResponseStream
        {
            get { return _responseStream; }
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

        internal UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* DataChunks
        {
            get
            {
                if (_dataChunks == null)
                {
                    return null;
                }
                else
                {
                    return (UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK*)(Marshal.UnsafeAddrOfPinnedArrayElement(_dataChunks, 0));
                }
            }
        }

        internal long FileLength
        {
            get { return _fileStream == null ? 0 : _fileStream.Length; }
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
            try
            {
                if (errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && errorCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
                {
                    asyncResult.Fail(new WebListenerException((int)errorCode));
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
                asyncResult.Fail(e);
            }
        }

        private static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
        {
            Overlapped callbackOverlapped = Overlapped.Unpack(nativeOverlapped);
            ResponseStreamAsyncResult asyncResult = callbackOverlapped.AsyncResult as ResponseStreamAsyncResult;

            IOCompleted(asyncResult, errorCode, numBytes);
        }

        internal void Complete()
        {
            if (_tcs.TrySetResult(null) && _callback != null)
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
            Dispose();
        }

        internal void Fail(Exception ex)
        {
            if (_tcs.TrySetException(ex) && _callback != null)
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
            Dispose();
        }

        /*++

            GetChunkHeader

            A private utility routine to convert an integer to a chunk header,
            which is an ASCII hex number followed by a CRLF. The header is retuned
            as a byte array.

            Input:

                size        - Chunk size to be encoded
                offset      - Out parameter where we store offset into buffer.

            Returns:

                A byte array with the header in int.

        --*/

        private static byte[] GetChunkHeader(int size, out int offset)
        {
            uint mask = 0xf0000000;
            byte[] header = new byte[10];
            int i;
            offset = -1;

            // Loop through the size, looking at each nibble. If it's not 0
            // convert it to hex. Save the index of the first non-zero
            // byte.

            for (i = 0; i < 8; i++, size <<= 4)
            {
                // offset == -1 means that we haven't found a non-zero nibble
                // yet. If we haven't found one, and the current one is zero,
                // don't do anything.

                if (offset == -1)
                {
                    if ((size & mask) == 0)
                    {
                        continue;
                    }
                }

                // Either we have a non-zero nibble or we're no longer skipping
                // leading zeros. Convert this nibble to ASCII and save it.

                uint temp = (uint)size >> 28;

                if (temp < 10)
                {
                    header[i] = (byte)(temp + '0');
                }
                else
                {
                    header[i] = (byte)((temp - 10) + 'A');
                }

                // If we haven't found a non-zero nibble yet, we've found one
                // now, so remember that.

                if (offset == -1)
                {
                    offset = i;
                }
            }

            header[8] = (byte)'\r';
            header[9] = (byte)'\n';

            return header;
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
        }
    }
}
