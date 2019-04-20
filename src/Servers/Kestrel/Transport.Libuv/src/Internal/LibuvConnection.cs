// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public partial class LibuvConnection : TransportConnection, IDisposable
    {
        private static readonly int MinAllocBufferSize = KestrelMemoryPool.MinimumSegmentSize / 2;

        private static readonly Action<UvStreamHandle, int, object> _readCallback =
            (handle, status, state) => ReadCallback(handle, status, state);

        private static readonly Func<UvStreamHandle, int, object, LibuvFunctions.uv_buf_t> _allocCallback =
            (handle, suggestedSize, state) => AllocCallback(handle, suggestedSize, state);

        private readonly UvStreamHandle _socket;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

        private volatile ConnectionAbortedException _abortReason;

        private MemoryHandle _bufferHandle;

        public LibuvConnection(UvStreamHandle socket, ILibuvTrace log, LibuvThread thread, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint)
        {
            _socket = socket;

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
                        // This is unexpected.
                        Log.ConnectionError(ConnectionId, ex);

                        inputError = ex;
                        outputError = ex;
                    }
                }
                finally
                {
                    inputError = inputError ?? _abortReason ?? new ConnectionAbortedException("The libuv transport's send loop completed gracefully.");

                    // Now, complete the input so that no more reads can happen
                    Input.Complete(inputError);
                    Output.Complete(outputError);

                    // Make sure it isn't possible for a paused read to resume reading after calling uv_close
                    // on the stream handle
                    Input.CancelPendingFlush();

                    // Send a FIN
                    Log.ConnectionWriteFin(ConnectionId, inputError.Message);

                    // We're done with the socket now
                    _socket.Dispose();
                    ThreadPool.UnsafeQueueUserWorkItem(state => ((LibuvConnection)state).CancelConnectionClosedToken(), this);
                }
            }
            catch (Exception e)
            {
                Log.LogCritical(0, e, $"{nameof(LibuvConnection)}.{nameof(Start)}() {ConnectionId}");
            }
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            _abortReason = abortReason;
            
            // Cancel WriteOutputAsync loop after setting _abortReason.
            Output.CancelPendingRead();

            // This cancels any pending I/O.
            Thread.Post(s => s.Dispose(), _socket);
        }

        // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
        public void Dispose()
        {
            _connectionClosedTokenSource.Dispose();
            _connectionClosingCts.Dispose();
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
                // This is unexpected.
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
