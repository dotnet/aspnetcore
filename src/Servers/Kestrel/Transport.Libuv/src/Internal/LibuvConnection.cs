// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public partial class LibuvConnection : TransportConnection
    {
        private static readonly int MinAllocBufferSize = KestrelMemoryPool.MinimumSegmentSize / 2;

        private static readonly Action<UvStreamHandle, int, object> _readCallback =
            (handle, status, state) => ReadCallback(handle, status, state);

        private static readonly Func<UvStreamHandle, int, object, LibuvFunctions.uv_buf_t> _allocCallback =
            (handle, suggestedsize, state) => AllocCallback(handle, suggestedsize, state);

        private readonly UvStreamHandle _socket;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private readonly bool _finOnError;

        private volatile ConnectionAbortedException _abortReason;

        private MemoryHandle _bufferHandle;

        public LibuvConnection(UvStreamHandle socket, ILibuvTrace log, LibuvThread thread, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint)
            : this(socket, log, thread, remoteEndPoint, localEndPoint, finOnError: false)
        {
        }

        internal LibuvConnection(UvStreamHandle socket, ILibuvTrace log, LibuvThread thread, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, bool finOnError)
        {
            _socket = socket;
            _finOnError = finOnError; ;

            RemoteAddress = remoteEndPoint?.Address;
            RemotePort = remoteEndPoint?.Port ?? 0;

            LocalAddress = localEndPoint?.Address;
            LocalPort = localEndPoint?.Port ?? 0;

            ConnectionClosed = _connectionClosedTokenSource.Token;
            Log = log;
            Thread = thread;
        }

        public LibuvOutputConsumer OutputConsumer { get; set; }
        private ILibuvTrace Log { get; }
        private LibuvThread Thread { get; }
        public override MemoryPool<byte> MemoryPool => Thread.MemoryPool;
        public override PipeScheduler InputWriterScheduler => Thread;
        public override PipeScheduler OutputReaderScheduler => Thread;

        public override long TotalBytesWritten => OutputConsumer?.TotalBytesWritten ?? 0;

        public async Task Start()
        {
            try
            {
                OutputConsumer = new LibuvOutputConsumer(Output, Thread, _socket, ConnectionId, Log);

                StartReading();

                Exception inputError = null;
                Exception outputError = null;

                try
                {
                    // This *must* happen after socket.ReadStart
                    // The socket output consumer is the only thing that can close the connection. If the
                    // output pipe is already closed by the time we start then it's fine since, it'll close gracefully afterwards.
                    await OutputConsumer.WriteOutputAsync();
                }
                catch (UvException ex)
                {
                    // The connection reset/error has already been logged by LibuvOutputConsumer
                    if (ex.StatusCode == LibuvConstants.ECANCELED)
                    {
                        // Connection was aborted.
                    }
                    else if (LibuvConstants.IsConnectionReset(ex.StatusCode))
                    {
                        // Don't cause writes to throw for connection resets.
                        inputError = new ConnectionResetException(ex.Message, ex);
                    }
                    else
                    {
                        inputError = ex;
                        outputError = ex;
                    }
                }
                finally
                {
                    if (!_finOnError && _abortReason != null)
                    {
                        // When shutdown isn't clean (note that we're using _abortReason, rather than inputError, to exclude that case),
                        // we set the DontLinger socket option to cause libuv to send a RST and release any buffered response data.
                        SetDontLingerOption(_socket);
                    }

                    // Now, complete the input so that no more reads can happen
                    Input.Complete(inputError ?? _abortReason ?? new ConnectionAbortedException());
                    Output.Complete(outputError);

                    // Make sure it isn't possible for a paused read to resume reading after calling uv_close
                    // on the stream handle
                    Input.CancelPendingFlush();

                    if (!_finOnError && _abortReason != null)
                    {
                        // Send a RST
                        Log.ConnectionWriteRst(ConnectionId);
                    }
                    else
                    {
                        // Send a FIN
                        Log.ConnectionWriteFin(ConnectionId);
                    }

                    // We're done with the socket now
                    _socket.Dispose();
                    ThreadPool.QueueUserWorkItem(state => ((LibuvConnection)state).CancelConnectionClosedToken(), this);
                }
            }
            catch (Exception e)
            {
                Log.LogCritical(0, e, $"{nameof(LibuvConnection)}.{nameof(Start)}() {ConnectionId}");
            }
        }

        /// <remarks>
        /// This should be called on <see cref="_socket"/> before it is disposed.
        /// Both <see cref="Abort"/> and <see cref="Start"/> call dispose but, rather than predict
        /// which will do so first (which varies), we make this method idempotent and call it in both.
        /// </remarks>
        private static void SetDontLingerOption(UvStreamHandle socket)
        {
            if (!socket.IsClosed && !socket.IsInvalid) {
                var libuv = socket.Libuv;
                if (libuv.IsWindows) // p/invoke of setsockopt is Windows-specific
                {
                    var pSocket = IntPtr.Zero;
                    libuv.uv_fileno(socket, ref pSocket);
                    libuv.setsockopt(pSocket, SocketOptionLevel.Socket, SocketOptionName.DontLinger, 0);
                }
            }
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            _abortReason = abortReason;
            Output.CancelPendingRead();

            Thread.Post(/*static*/ (self) =>
            {
                if (!self._finOnError)
                {
                    SetDontLingerOption(self._socket);
                }

                // This cancels any pending I/O.
                self._socket.Dispose();
            }, this);
        }

        // Called on Libuv thread
        private static LibuvFunctions.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((LibuvConnection)state).OnAlloc(handle, suggestedSize);
        }

        private unsafe LibuvFunctions.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            var currentWritableBuffer = Input.GetMemory(MinAllocBufferSize);
            _bufferHandle = currentWritableBuffer.Pin();

            return handle.Libuv.buf_init((IntPtr)_bufferHandle.Pointer, currentWritableBuffer.Length);
        }

        private static void ReadCallback(UvStreamHandle handle, int status, object state)
        {
            ((LibuvConnection)state).OnRead(handle, status);
        }

        private void OnRead(UvStreamHandle handle, int status)
        {
            // Cleanup state from last OnAlloc. This is safe even if OnAlloc wasn't called.
            _bufferHandle.Dispose();
            if (status == 0)
            {
                // EAGAIN/EWOULDBLOCK so just return the buffer.
                // http://docs.libuv.org/en/v1.x/stream.html#c.uv_read_cb
            }
            else if (status > 0)
            {
                Log.ConnectionRead(ConnectionId, status);

                Input.Advance(status);
                var flushTask = Input.FlushAsync();

                if (!flushTask.IsCompleted)
                {
                    // We wrote too many bytes to the reader, so pause reading and resume when
                    // we hit the low water mark.
                    _ = ApplyBackpressureAsync(flushTask);
                }
            }
            else
            {
                // Given a negative status, it's possible that OnAlloc wasn't called.
                _socket.ReadStop();

                Exception error = null;

                if (status == LibuvConstants.EOF)
                {
                    Log.ConnectionReadFin(ConnectionId);
                }
                else
                {
                    handle.Libuv.Check(status, out var uvError);
                    error = LogAndWrapReadError(uvError);
                }

                // Complete after aborting the connection
                Input.Complete(error);
            }
        }

        private async Task ApplyBackpressureAsync(ValueTask<FlushResult> flushTask)
        {
            Log.ConnectionPause(ConnectionId);
            _socket.ReadStop();

            var result = await flushTask;

            // If the reader isn't complete or cancelled then resume reading
            if (!result.IsCompleted && !result.IsCanceled)
            {
                Log.ConnectionResume(ConnectionId);
                StartReading();
            }
        }

        private void StartReading()
        {
            try
            {
                _socket.ReadStart(_allocCallback, _readCallback, this);
            }
            catch (UvException ex)
            {
                // ReadStart() can throw a UvException in some cases (e.g. socket is no longer connected).
                // This should be treated the same as OnRead() seeing a negative status.
                Input.Complete(LogAndWrapReadError(ex));
            }
        }

        private Exception LogAndWrapReadError(UvException uvError)
        {
            if (uvError.StatusCode == LibuvConstants.ECANCELED)
            {
                // The operation was canceled by the server not the client. No need for additional logs.
                return new ConnectionAbortedException(uvError.Message, uvError);
            }
            else if (LibuvConstants.IsConnectionReset(uvError.StatusCode))
            {
                // Log connection resets at a lower (Debug) level.
                Log.ConnectionReset(ConnectionId);
                return new ConnectionResetException(uvError.Message, uvError);
            }
            else
            {
                Log.ConnectionError(ConnectionId, uvError);
                return new IOException(uvError.Message, uvError);
            }
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Unexpected exception in {nameof(LibuvConnection)}.{nameof(CancelConnectionClosedToken)}.");
            }
        }
    }
}
