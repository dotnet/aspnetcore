// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Adapter;
using Microsoft.AspNetCore.Server.Kestrel.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class Connection : ConnectionContext, IConnectionControl
    {
        private const int MinAllocBufferSize = 2048;

        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        private static readonly Action<UvStreamHandle, int, object> _readCallback =
            (handle, status, state) => ReadCallback(handle, status, state);

        private static readonly Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback =
            (handle, suggestedsize, state) => AllocCallback(handle, suggestedsize, state);

        // Seed the _lastConnectionId for this application instance with
        // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
        // for a roughly increasing _requestId over restarts
        private static long _lastConnectionId = DateTime.UtcNow.Ticks;

        private readonly UvStreamHandle _socket;
        private readonly Frame _frame;
        private readonly List<IConnectionAdapter> _connectionAdapters;
        private AdaptedPipeline _adaptedPipeline;
        private Stream _filteredStream;
        private Task _readInputTask;

        private TaskCompletionSource<object> _socketClosedTcs = new TaskCompletionSource<object>();

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutAction _timeoutAction;
        private WritableBuffer? _currentWritableBuffer;

        public Connection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            _connectionAdapters = context.ListenOptions.ConnectionAdapters;
            socket.Connection = this;
            ConnectionControl = this;

            ConnectionId = GenerateConnectionId(Interlocked.Increment(ref _lastConnectionId));

            Input = Thread.PipelineFactory.Create(ListenerContext.LibuvInputPipeOptions);
            var outputPipe = Thread.PipelineFactory.Create(ListenerContext.LibuvOutputPipeOptions);
            Output = new SocketOutput(outputPipe, Thread, _socket, this, ConnectionId, Log);

            var tcpHandle = _socket as UvTcpHandle;
            if (tcpHandle != null)
            {
                RemoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                LocalEndPoint = tcpHandle.GetSockIPEndPoint();
            }

            _frame = FrameFactory(this);
            _lastTimestamp = Thread.Loop.Now();
        }

        // Internal for testing
        internal Connection()
        {
        }

        public KestrelServerOptions ServerOptions => ListenerContext.ServiceContext.ServerOptions;
        private Func<ConnectionContext, Frame> FrameFactory => ListenerContext.ServiceContext.FrameFactory;
        private IKestrelTrace Log => ListenerContext.ServiceContext.Log;
        private IThreadPool ThreadPool => ListenerContext.ServiceContext.ThreadPool;
        private KestrelThread Thread => ListenerContext.Thread;

        public void Start()
        {
            Log.ConnectionStart(ConnectionId);
            KestrelEventSource.Log.ConnectionStart(this);

            // Start socket prior to applying the ConnectionAdapter
            _socket.ReadStart(_allocCallback, _readCallback, this);

            // Dispatch to a thread pool so if the first read completes synchronously
            // we won't be on IO thread
            try
            {
                ThreadPool.UnsafeRun(state => ((Connection)state).StartFrame(), this);
            }
            catch (Exception e)
            {
                Log.LogError(0, e, "Connection.StartFrame");
                throw;
            }
        }

        private void StartFrame()
        {
            if (_connectionAdapters.Count == 0)
            {
                _frame.Start();
            }
            else
            {
                // ApplyConnectionAdaptersAsync should never throw. If it succeeds, it will call _frame.Start().
                // Otherwise, it will close the connection.
                var ignore = ApplyConnectionAdaptersAsync();
            }
        }

        public Task StopAsync()
        {
            return Task.WhenAll(_frame.StopAsync(), _socketClosedTcs.Task);
        }

        public virtual Task AbortAsync(Exception error = null)
        {
            // Frame.Abort calls user code while this method is always
            // called from a libuv thread.
            ThreadPool.Run(() =>
            {
                _frame.Abort(error);
            });

            return _socketClosedTcs.Task;
        }

        // Called on Libuv thread
        public virtual void OnSocketClosed()
        {
            KestrelEventSource.Log.ConnectionStop(this);

            _frame.FrameStartedTask.ContinueWith((task, state) =>
            {
                var connection = (Connection)state;

                if (connection._adaptedPipeline != null)
                {
                    Task.WhenAll(connection._readInputTask, connection._frame.StopAsync()).ContinueWith((task2, state2) =>
                    {
                        var connection2 = (Connection)state2;
                        connection2._filteredStream.Dispose();
                        connection2._adaptedPipeline.Dispose();
                        Input.Reader.Complete();
                    }, connection);
                }
            }, this);

            Input.Writer.Complete(new TaskCanceledException("The request was aborted"));
            _socketClosedTcs.TrySetResult(null);
        }

        // Called on Libuv thread
        public void Tick(long timestamp)
        {
            if (timestamp > PlatformApis.VolatileRead(ref _timeoutTimestamp))
            {
                ConnectionControl.CancelTimeout();

                if (_timeoutAction == TimeoutAction.SendTimeoutResponse)
                {
                    _frame.SetBadRequestState(RequestRejectionReason.RequestTimeout);
                }

                StopAsync();
            }

            Interlocked.Exchange(ref _lastTimestamp, timestamp);
        }

        private async Task ApplyConnectionAdaptersAsync()
        {
            try
            {
                var rawStream = new RawStream(Input.Reader, Output);
                var adapterContext = new ConnectionAdapterContext(rawStream);
                var adaptedConnections = new IAdaptedConnection[_connectionAdapters.Count];

                for (var i = 0; i < _connectionAdapters.Count; i++)
                {
                    var adaptedConnection = await _connectionAdapters[i].OnConnectionAsync(adapterContext);
                    adaptedConnections[i] = adaptedConnection;
                    adapterContext = new ConnectionAdapterContext(adaptedConnection.ConnectionStream);
                }

                if (adapterContext.ConnectionStream != rawStream)
                {
                    _filteredStream = adapterContext.ConnectionStream;
                    _adaptedPipeline = new AdaptedPipeline(
                        adapterContext.ConnectionStream,
                        Thread.PipelineFactory.Create(ListenerContext.AdaptedPipeOptions),
                        Thread.PipelineFactory.Create(ListenerContext.AdaptedPipeOptions));

                    _frame.Input = _adaptedPipeline.Input;
                    _frame.Output = _adaptedPipeline.Output;

                    // Don't attempt to read input if connection has already closed.
                    // This can happen if a client opens a connection and immediately closes it.
                    _readInputTask = _socketClosedTcs.Task.Status == TaskStatus.WaitingForActivation
                        ? _adaptedPipeline.StartAsync()
                        : TaskCache.CompletedTask;
                }

                _frame.AdaptedConnections = adaptedConnections;
                _frame.Start();
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Uncaught exception from the {nameof(IConnectionAdapter.OnConnectionAsync)} method of an {nameof(IConnectionAdapter)}.");
                Input.Reader.Complete();
                ConnectionControl.End(ProduceEndType.SocketDisconnect);
            }
        }

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((Connection)state).OnAlloc(handle, suggestedSize);
        }

        private unsafe Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            Debug.Assert(_currentWritableBuffer == null);
            var currentWritableBuffer = Input.Writer.Alloc(MinAllocBufferSize);
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
            ((Connection)state).OnRead(handle, status);
        }

        private async void OnRead(UvStreamHandle handle, int status)
        {
            var normalRead = status >= 0;
            var normalDone = status == Constants.EOF;
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
                if (status == Constants.ECONNRESET)
                {
                    Log.ConnectionReset(ConnectionId);
                }
                else
                {
                    Log.ConnectionError(ConnectionId, uvError);
                }

                error = new IOException(uvError.Message, uvError);
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
                OnPausePosted();
                var result = await flushTask.Value;
                // If the reader isn't complete then resume
                if (!result.IsCompleted)
                {
                    OnResumePosted();
                }
            }

            if (!normalRead)
            {
                Input.Writer.Complete(error);
                var ignore = AbortAsync(error);
            }
        }

        void IConnectionControl.Pause()
        {
            Log.ConnectionPause(ConnectionId);

            // Even though this method is called on the event loop already,
            // post anyway so the ReadStop() call doesn't get reordered
            // relative to the ReadStart() call made in Resume().
            Thread.Post(state => state.OnPausePosted(), this);
        }

        void IConnectionControl.Resume()
        {
            Log.ConnectionResume(ConnectionId);

            // This is called from the consuming thread.
            Thread.Post(state => state.OnResumePosted(), this);
        }

        private void OnPausePosted()
        {
            // It's possible that uv_close was called between the call to Thread.Post() and now.
            if (!_socket.IsClosed)
            {
                _socket.ReadStop();
            }
        }

        private void OnResumePosted()
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
                    Input.Writer.Complete();
                }
            }
        }

        void IConnectionControl.End(ProduceEndType endType)
        {
            switch (endType)
            {
                case ProduceEndType.ConnectionKeepAlive:
                    Log.ConnectionKeepAlive(ConnectionId);
                    break;
                case ProduceEndType.SocketShutdown:
                case ProduceEndType.SocketDisconnect:
                    Log.ConnectionDisconnect(ConnectionId);
                    ((SocketOutput)Output).End(endType);
                    break;
            }
        }

        void IConnectionControl.SetTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
            Debug.Assert(_timeoutTimestamp == long.MaxValue, "Concurrent timeouts are not supported");

            AssignTimeout(milliseconds, timeoutAction);
        }

        void IConnectionControl.ResetTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
            AssignTimeout(milliseconds, timeoutAction);
        }

        void IConnectionControl.CancelTimeout()
        {
            Interlocked.Exchange(ref _timeoutTimestamp, long.MaxValue);
        }

        private void AssignTimeout(long milliseconds, TimeoutAction timeoutAction)
        {
            _timeoutAction = timeoutAction;

            // Add KestrelThread.HeartbeatMilliseconds extra milliseconds since this can be called right before the next heartbeat.
            Interlocked.Exchange(ref _timeoutTimestamp, _lastTimestamp + milliseconds + KestrelThread.HeartbeatMilliseconds);
        }

        private static unsafe string GenerateConnectionId(long id)
        {
            // The following routine is ~310% faster than calling long.ToString() on x64
            // and ~600% faster than calling long.ToString() on x86 in tight loops of 1 million+ iterations
            // See: https://github.com/aspnet/Hosting/pull/385

            // stackalloc to allocate array on stack rather than heap
            char* charBuffer = stackalloc char[13];

            charBuffer[0] = _encode32Chars[(int)(id >> 60) & 31];
            charBuffer[1] = _encode32Chars[(int)(id >> 55) & 31];
            charBuffer[2] = _encode32Chars[(int)(id >> 50) & 31];
            charBuffer[3] = _encode32Chars[(int)(id >> 45) & 31];
            charBuffer[4] = _encode32Chars[(int)(id >> 40) & 31];
            charBuffer[5] = _encode32Chars[(int)(id >> 35) & 31];
            charBuffer[6] = _encode32Chars[(int)(id >> 30) & 31];
            charBuffer[7] = _encode32Chars[(int)(id >> 25) & 31];
            charBuffer[8] = _encode32Chars[(int)(id >> 20) & 31];
            charBuffer[9] = _encode32Chars[(int)(id >> 15) & 31];
            charBuffer[10] = _encode32Chars[(int)(id >> 10) & 31];
            charBuffer[11] = _encode32Chars[(int)(id >> 5) & 31];
            charBuffer[12] = _encode32Chars[(int)id & 31];

            // string ctor overload that takes char*
            return new string(charBuffer, 0, 13);
        }
    }
}
