// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class Connection : ConnectionContext, IConnectionControl
    {
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
        private Frame _frame;
        private ConnectionFilterContext _filterContext;
        private LibuvStream _libuvStream;
        private FilteredStreamAdapter _filteredStreamAdapter;
        private Task _readInputTask;

        private readonly SocketInput _rawSocketInput;
        private readonly SocketOutput _rawSocketOutput;

        private readonly object _stateLock = new object();
        private ConnectionState _connectionState;
        private TaskCompletionSource<object> _socketClosedTcs;

        bool _eConnResetChecked = false;

        public Connection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            socket.Connection = this;
            ConnectionControl = this;

            ConnectionId = GenerateConnectionId(Interlocked.Increment(ref _lastConnectionId));

            _rawSocketInput = new SocketInput(Memory, ThreadPool);
            _rawSocketOutput = new SocketOutput(Thread, _socket, Memory, this, ConnectionId, Log, ThreadPool, WriteReqPool);
        }

        // Internal for testing
        internal Connection()
        {
        }

        public void Start()
        {
            Log.ConnectionStart(ConnectionId);

            // Start socket prior to applying the ConnectionFilter
            _socket.ReadStart(_allocCallback, _readCallback, this);

            var tcpHandle = _socket as UvTcpHandle;
            if (tcpHandle != null)
            {
                RemoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                LocalEndPoint = tcpHandle.GetSockIPEndPoint();
            }

            // Don't initialize _frame until SocketInput and SocketOutput are set to their final values.
            if (ServerOptions.ConnectionFilter == null)
            {
                lock (_stateLock)
                {
                    if (_connectionState != ConnectionState.CreatingFrame)
                    {
                        throw new InvalidOperationException("Invalid connection state: " + _connectionState);
                    }

                    _connectionState = ConnectionState.Open;

                    SocketInput = _rawSocketInput;
                    SocketOutput = _rawSocketOutput;

                    _frame = CreateFrame();
                    _frame.Start();
                }
            }
            else
            {
                _libuvStream = new LibuvStream(_rawSocketInput, _rawSocketOutput);

                _filterContext = new ConnectionFilterContext
                {
                    Connection = _libuvStream,
                    Address = ServerAddress
                };

                try
                {
                    ServerOptions.ConnectionFilter.OnConnectionAsync(_filterContext).ContinueWith((task, state) =>
                    {
                        var connection = (Connection)state;

                        if (task.IsFaulted)
                        {
                            connection.Log.LogError(0, task.Exception, "ConnectionFilter.OnConnection");
                            connection.ConnectionControl.End(ProduceEndType.SocketDisconnect);
                        }
                        else if (task.IsCanceled)
                        {
                            connection.Log.LogError("ConnectionFilter.OnConnection Canceled");
                            connection.ConnectionControl.End(ProduceEndType.SocketDisconnect);
                        }
                        else
                        {
                            connection.ApplyConnectionFilter();
                        }
                    }, this);
                }
                catch (Exception ex)
                {
                    Log.LogError(0, ex, "ConnectionFilter.OnConnection");
                    ConnectionControl.End(ProduceEndType.SocketDisconnect);
                }
            }
        }

        public Task StopAsync()
        {
            lock (_stateLock)
            {
                switch (_connectionState)
                {
                    case ConnectionState.SocketClosed:
                        return TaskUtilities.CompletedTask;
                    case ConnectionState.CreatingFrame:
                        _connectionState = ConnectionState.ToDisconnect;
                        break;
                    case ConnectionState.Open:
                        _frame.Stop();
                        SocketInput.CompleteAwaiting();
                        break;
                }

                _socketClosedTcs = new TaskCompletionSource<object>();
                return _socketClosedTcs.Task;
            }
        }

        public virtual void Abort()
        {
            // Frame.Abort calls user code while this method is always
            // called from a libuv thread.
            ThreadPool.Run(() =>
            {
                var connection = this;

                lock (connection._stateLock)
                {
                    if (connection._connectionState == ConnectionState.CreatingFrame)
                    {
                        connection._connectionState = ConnectionState.ToDisconnect;
                    }
                    else
                    {
                        connection._frame?.Abort();
                    }
                }
            });
        }

        // Called on Libuv thread
        public virtual void OnSocketClosed()
        {
            if (_filteredStreamAdapter != null)
            {
                _filteredStreamAdapter.Abort();
                _rawSocketInput.IncomingFin();
                _readInputTask.ContinueWith((task, state) =>
                {
                    ((Connection)state)._filterContext.Connection.Dispose();
                    ((Connection)state)._filteredStreamAdapter.Dispose();
                    ((Connection)state)._rawSocketInput.Dispose();
                }, this);
            }
            else
            {
                _rawSocketInput.Dispose();
            }

            lock (_stateLock)
            {
                _connectionState = ConnectionState.SocketClosed;

                if (_socketClosedTcs != null)
                {
                    // This is always waited on synchronously, so it's safe to
                    // call on the libuv thread. 
                    _socketClosedTcs.TrySetResult(null);
                }
            }
        }

        private void ApplyConnectionFilter()
        {
            lock (_stateLock)
            {
                if (_connectionState == ConnectionState.CreatingFrame)
                {
                    _connectionState = ConnectionState.Open;

                    if (_filterContext.Connection != _libuvStream)
                    {
                        _filteredStreamAdapter = new FilteredStreamAdapter(ConnectionId, _filterContext.Connection, Memory, Log, ThreadPool);

                        SocketInput = _filteredStreamAdapter.SocketInput;
                        SocketOutput = _filteredStreamAdapter.SocketOutput;

                        _readInputTask = _filteredStreamAdapter.ReadInputAsync();
                    }
                    else
                    {
                        SocketInput = _rawSocketInput;
                        SocketOutput = _rawSocketOutput;
                    }

                    PrepareRequest = _filterContext.PrepareRequest;

                    _frame = CreateFrame();
                    _frame.Start();
                }
                else
                {
                    ConnectionControl.End(ProduceEndType.SocketDisconnect);
                }
            }
        }

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((Connection)state).OnAlloc(handle, suggestedSize);
        }

        private Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            var result = _rawSocketInput.IncomingStart();

            return handle.Libuv.buf_init(
                result.Pin() + result.End,
                result.Data.Offset + result.Data.Count - result.End);
        }

        private static void ReadCallback(UvStreamHandle handle, int status, object state)
        {
            ((Connection)state).OnRead(handle, status);
        }

        private void OnRead(UvStreamHandle handle, int status)
        {
            if (status == 0)
            {
                // A zero status does not indicate an error or connection end. It indicates
                // there is no data to be read right now.
                // See the note at http://docs.libuv.org/en/v1.x/stream.html#c.uv_read_cb.
                // We need to clean up whatever was allocated by OnAlloc.
                _rawSocketInput.IncomingDeferred();
                return;
            }

            if (!_eConnResetChecked && !Constants.ECONNRESET.HasValue)
            {
                Log.LogWarning("Unable to determine ECONNRESET value on this platform.");
                _eConnResetChecked = true;
            }

            var normalRead = status > 0;
            var normalDone = status == Constants.ECONNRESET || status == Constants.EOF;
            var errorDone = !(normalDone || normalRead);
            var readCount = normalRead ? status : 0;

            if (normalRead)
            {
                Log.ConnectionRead(ConnectionId, readCount);
            }
            else
            {
                _socket.ReadStop();
                Log.ConnectionReadFin(ConnectionId);
            }

            Exception error = null;
            if (errorDone)
            {
                handle.Libuv.Check(status, out error);
            }

            _rawSocketInput.IncomingComplete(readCount, error);

            if (errorDone)
            {
                Abort();
            }
        }

        private Frame CreateFrame()
        {
            return FrameFactory(this);
        }

        void IConnectionControl.Pause()
        {
            Log.ConnectionPause(ConnectionId);
            _socket.ReadStop();
        }

        void IConnectionControl.Resume()
        {
            Log.ConnectionResume(ConnectionId);
            _socket.ReadStart(_allocCallback, _readCallback, this);
        }

        void IConnectionControl.End(ProduceEndType endType)
        {
            lock (_stateLock)
            {
                switch (endType)
                {
                    case ProduceEndType.ConnectionKeepAlive:
                        if (_connectionState != ConnectionState.Open)
                        {
                            return;
                        }

                        Log.ConnectionKeepAlive(ConnectionId);
                        break;
                    case ProduceEndType.SocketShutdown:
                    case ProduceEndType.SocketDisconnect:
                        if (_connectionState == ConnectionState.Disconnecting ||
                            _connectionState == ConnectionState.SocketClosed)
                        {
                            return;
                        }
                        _connectionState = ConnectionState.Disconnecting;

                        Log.ConnectionDisconnect(ConnectionId);
                        _rawSocketOutput.End(endType);
                        break;
                }
            }
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

        private enum ConnectionState
        {
            CreatingFrame,
            ToDisconnect,
            Open,
            Disconnecting,
            SocketClosed
        }
    }
}
