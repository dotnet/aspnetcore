// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public partial class LibuvConnection : LibuvConnectionContext
    {
        private const int MinAllocBufferSize = 2048;

        private static readonly Action<UvStreamHandle, int, object> _readCallback =
            (handle, status, state) => ReadCallback(handle, status, state);

        private static readonly Func<UvStreamHandle, int, object, LibuvFunctions.uv_buf_t> _allocCallback =
            (handle, suggestedsize, state) => AllocCallback(handle, suggestedsize, state);

        private readonly UvStreamHandle _socket;

        private WritableBuffer? _currentWritableBuffer;
        private BufferHandle _bufferHandle;

        public LibuvConnection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;

            if (_socket is UvTcpHandle tcpHandle)
            {
                var remoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                var localEndPoint = tcpHandle.GetSockIPEndPoint();

                RemoteAddress = remoteEndPoint.Address;
                RemotePort = remoteEndPoint.Port;

                LocalAddress = localEndPoint.Address;
                LocalPort = localEndPoint.Port;
            }
        }

        public LibuvOutputConsumer OutputConsumer { get; set; }

        private ILibuvTrace Log => ListenerContext.TransportContext.Log;
        private IConnectionHandler ConnectionHandler => ListenerContext.TransportContext.ConnectionHandler;
        private LibuvThread Thread => ListenerContext.Thread;

        public async Task Start()
        {
            try
            {
                ConnectionHandler.OnConnection(this);

                OutputConsumer = new LibuvOutputConsumer(Output, Thread, _socket, ConnectionId, Log);

                StartReading();

                Exception error = null;

                try
                {
                    // This *must* happen after socket.ReadStart
                    // The socket output consumer is the only thing that can close the connection. If the
                    // output pipe is already closed by the time we start then it's fine since, it'll close gracefully afterwards.
                    await OutputConsumer.WriteOutputAsync();
                }
                catch (UvException ex)
                {
                    error = new IOException(ex.Message, ex);
                }
                finally
                {
                    // Now, complete the input so that no more reads can happen
                    Input.Complete(error ?? new ConnectionAbortedException());
                    Output.Complete(error);

                    // Make sure it isn't possible for a paused read to resume reading after calling uv_close
                    // on the stream handle
                    Input.CancelPendingFlush();

                    // Send a FIN
                    Log.ConnectionWriteFin(ConnectionId);

                    // We're done with the socket now
                    _socket.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.LogCritical(0, e, $"{nameof(LibuvConnection)}.{nameof(Start)}() {ConnectionId}");
            }
        }

        // Called on Libuv thread
        private static LibuvFunctions.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((LibuvConnection)state).OnAlloc(handle, suggestedSize);
        }

        private unsafe LibuvFunctions.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            Debug.Assert(_currentWritableBuffer == null);
            var currentWritableBuffer = Input.Alloc(MinAllocBufferSize);
            _currentWritableBuffer = currentWritableBuffer;

            _bufferHandle = currentWritableBuffer.Buffer.Retain(true);

            return handle.Libuv.buf_init((IntPtr)_bufferHandle.PinnedPointer, currentWritableBuffer.Buffer.Length);
        }

        private static void ReadCallback(UvStreamHandle handle, int status, object state)
        {
            ((LibuvConnection)state).OnRead(handle, status);
        }

        private void OnRead(UvStreamHandle handle, int status)
        {
            if (status == 0)
            {
                // EAGAIN/EWOULDBLOCK so just return the buffer.
                // http://docs.libuv.org/en/v1.x/stream.html#c.uv_read_cb
                Debug.Assert(_currentWritableBuffer != null);
                _currentWritableBuffer.Value.Commit();
            }
            else if (status > 0)
            {
                Log.ConnectionRead(ConnectionId, status);

                Debug.Assert(_currentWritableBuffer != null);
                var currentWritableBuffer = _currentWritableBuffer.Value;
                currentWritableBuffer.Advance(status);
                var flushTask = currentWritableBuffer.FlushAsync();

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
                _currentWritableBuffer?.Commit();
                _socket.ReadStop();

                IOException error = null;

                if (status == LibuvConstants.EOF)
                {
                    Log.ConnectionReadFin(ConnectionId);
                }
                else
                {
                    handle.Libuv.Check(status, out var uvError);

                    // Log connection resets at a lower (Debug) level.
                    if (status == LibuvConstants.ECONNRESET)
                    {
                        Log.ConnectionReset(ConnectionId);
                        error = new ConnectionResetException(uvError.Message, uvError);
                    }
                    else
                    {
                        Log.ConnectionError(ConnectionId, uvError);
                        error = new IOException(uvError.Message, uvError);
                    }
                }

                // Complete after aborting the connection
                Input.Complete(error);
            }

            // Cleanup state from last OnAlloc. This is safe even if OnAlloc wasn't called.
            _currentWritableBuffer = null;
            _bufferHandle.Dispose();
        }

        private async Task ApplyBackpressureAsync(WritableBufferAwaitable flushTask)
        {
            Log.ConnectionPause(ConnectionId);
            _socket.ReadStop();

            var result = await flushTask;

            // If the reader isn't complete or cancelled then resume reading
            if (!result.IsCompleted && !result.IsCancelled)
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
                Log.ConnectionReadFin(ConnectionId);
                var error = new IOException(ex.Message, ex);

                Input.Complete(error);
            }
        }
    }
}
