// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public class LibuvConnection : LibuvConnectionContext, ITimeoutControl
    {
        private const int MinAllocBufferSize = 2048;

        private static readonly Action<UvStreamHandle, int, object> _readCallback =
            (handle, status, state) => ReadCallback(handle, status, state);

        private static readonly Func<UvStreamHandle, int, object, LibuvFunctions.uv_buf_t> _allocCallback =
            (handle, suggestedsize, state) => AllocCallback(handle, suggestedsize, state);

        private readonly UvStreamHandle _socket;
        private IConnectionContext _connectionContext;

        private TaskCompletionSource<object> _socketClosedTcs = new TaskCompletionSource<object>();

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutAction _timeoutAction;
        private WritableBuffer? _currentWritableBuffer;

        public LibuvConnection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            socket.Connection = this;
            TimeoutControl = this;

            var tcpHandle = _socket as UvTcpHandle;
            if (tcpHandle != null)
            {
                RemoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                LocalEndPoint = tcpHandle.GetSockIPEndPoint();
            }

            _lastTimestamp = Thread.Loop.Now();
        }

        // For testing
        public LibuvConnection()
        {
        }

        public string ConnectionId { get; set; }
        public IPipeWriter Input { get; set; }
        public LibuvOutputConsumer Output { get; set; }

        private ILibuvTrace Log => ListenerContext.TransportContext.Log;
        private IConnectionHandler ConnectionHandler => ListenerContext.TransportContext.ConnectionHandler;
        private LibuvThread Thread => ListenerContext.Thread;

        public void Start()
        {
            try
            {
                _connectionContext = ConnectionHandler.OnConnection(this);
                ConnectionId = _connectionContext.ConnectionId;

                Input = _connectionContext.Input;
                Output = new LibuvOutputConsumer(_connectionContext.Output, Thread, _socket, this, ConnectionId, Log);

                // Start socket prior to applying the ConnectionAdapter
                _socket.ReadStart(_allocCallback, _readCallback, this);
                _lastTimestamp = Thread.Loop.Now();

                // This *must* happen after socket.ReadStart
                // The socket output consumer is the only thing that can close the connection. If the
                // output pipe is already closed by the time we start then it's fine since, it'll close gracefully afterwards.
                var ignore = Output.StartWrites();
            }
            catch (Exception e)
            {
                Log.LogError(0, e, "Connection.StartFrame");
                throw;
            }
        }

        public Task StopAsync()
        {
            return Task.WhenAll(_connectionContext.StopAsync(), _socketClosedTcs.Task);
        }

        public virtual Task AbortAsync(Exception error = null)
        {
            _connectionContext.Abort(error);
            return _socketClosedTcs.Task;
        }

        // Called on Libuv thread
        public virtual void Close()
        {
            _socket.Dispose();

            _connectionContext.OnConnectionClosed();

            Input.Complete(new TaskCanceledException("The request was aborted"));
            _socketClosedTcs.TrySetResult(null);
        }

        // Called on Libuv thread
        public void Tick(long timestamp)
        {
            if (timestamp > PlatformApis.VolatileRead(ref _timeoutTimestamp))
            {
                TimeoutControl.CancelTimeout();

                if (_timeoutAction == TimeoutAction.SendTimeoutResponse)
                {
                    _connectionContext.Timeout();
                }

                StopAsync();
            }

            Interlocked.Exchange(ref _lastTimestamp, timestamp);
        }

        private static LibuvFunctions.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((LibuvConnection)state).OnAlloc(handle, suggestedSize);
        }

        private unsafe LibuvFunctions.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            Debug.Assert(_currentWritableBuffer == null);
            var currentWritableBuffer = Input.Alloc(MinAllocBufferSize);
            _currentWritableBuffer = currentWritableBuffer;
            void* dataPtr;
            var tryGetPointer = currentWritableBuffer.Buffer.TryGetPointer(out dataPtr);
            Debug.Assert(tryGetPointer);

            return handle.Libuv.buf_init(
                (IntPtr)dataPtr,
                currentWritableBuffer.Buffer.Length);
        }

        private static void ReadCallback(UvStreamHandle handle, int status, object state)
        {
            ((LibuvConnection)state).OnRead(handle, status);
        }

        private async void OnRead(UvStreamHandle handle, int status)
        {
            var normalRead = status >= 0;
            var normalDone = status == LibuvConstants.EOF;
            var errorDone = !(normalDone || normalRead);
            var readCount = normalRead ? status : 0;

            if (normalRead)
            {
                Log.ConnectionRead(ConnectionId, readCount);
            }
            else
            {
                _socket.ReadStop();

                if (normalDone)
                {
                    Log.ConnectionReadFin(ConnectionId);
                }
            }

            IOException error = null;
            WritableBufferAwaitable? flushTask = null;
            if (errorDone)
            {
                Exception uvError;
                handle.Libuv.Check(status, out uvError);

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

                _currentWritableBuffer?.Commit();
            }
            else
            {
                Debug.Assert(_currentWritableBuffer != null);

                var currentWritableBuffer = _currentWritableBuffer.Value;
                currentWritableBuffer.Advance(readCount);
                flushTask = currentWritableBuffer.FlushAsync();
            }

            _currentWritableBuffer = null;
            if (flushTask?.IsCompleted == false)
            {
                Pause();
                var result = await flushTask.Value;
                // If the reader isn't complete then resume
                if (!result.IsCompleted)
                {
                    Resume();
                }
            }

            if (!normalRead)
            {
                Input.Complete(error);
                var ignore = AbortAsync(error);
            }
        }

        private void Pause()
        {
            // It's possible that uv_close was called between the call to Thread.Post() and now.
            if (!_socket.IsClosed)
            {
                _socket.ReadStop();
            }
        }

        private void Resume()
        {
            // It's possible that uv_close was called even before the call to Resume().
            if (!_socket.IsClosed)
            {
                try
                {
                    _socket.ReadStart(_allocCallback, _readCallback, this);
                }
                catch (UvException)
                {
                    // ReadStart() can throw a UvException in some cases (e.g. socket is no longer connected).
                    // This should be treated the same as OnRead() seeing a "normalDone" condition.
                    Log.ConnectionReadFin(ConnectionId);
                    Input.Complete();
                }
            }
        }

        void ITimeoutControl.SetTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
            Debug.Assert(_timeoutTimestamp == long.MaxValue, "Concurrent timeouts are not supported");

            AssignTimeout(milliseconds, timeoutAction);
        }

        void ITimeoutControl.ResetTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
            AssignTimeout(milliseconds, timeoutAction);
        }

        void ITimeoutControl.CancelTimeout()
        {
            Interlocked.Exchange(ref _timeoutTimestamp, long.MaxValue);
        }

        private void AssignTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
            _timeoutAction = timeoutAction;

            // Add LibuvThread.HeartbeatMilliseconds extra milliseconds since this can be called right before the next heartbeat.
            Interlocked.Exchange(ref _timeoutTimestamp, _lastTimestamp + milliseconds + LibuvThread.HeartbeatMilliseconds);
        }
    }
}
