// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
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
        private static readonly Action<UvStreamHandle, int, object> _readCallback = 
            (handle, status, state) => ReadCallback(handle, status, state);
        private static readonly Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback = 
            (handle, suggestedsize, state) => AllocCallback(handle, suggestedsize, state);

        private static long _lastConnectionId;

        private readonly UvStreamHandle _socket;
        private Frame _frame;
        private ConnectionFilterContext _filterContext;
        private LibuvStream _libuvStream;
        private readonly long _connectionId;

        private readonly SocketInput _rawSocketInput;
        private readonly SocketOutput _rawSocketOutput;

        private readonly object _stateLock = new object();
        private ConnectionState _connectionState;
        private TaskCompletionSource<object> _socketClosedTcs;
        private Task _readFilteredInputTask = TaskUtilities.CompletedTask;

        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;

        public Connection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            ConnectionControl = this;

            _connectionId = Interlocked.Increment(ref _lastConnectionId);

            _rawSocketInput = new SocketInput(Memory2, ThreadPool);
            _rawSocketOutput = new SocketOutput(Thread, _socket, Memory2, this, _connectionId, Log, ThreadPool, WriteReqPool);

            ConnectionManager.AddConnection(_connectionId, this);
        }

        // Internal for testing
        internal Connection()
        {
        }

        public void Start()
        {
            Log.ConnectionStart(_connectionId);

            // Start socket prior to applying the ConnectionFilter
            _socket.ReadStart(_allocCallback, _readCallback, this);

            var tcpHandle = _socket as UvTcpHandle;
            if (tcpHandle != null)
            {
                _remoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                _localEndPoint = tcpHandle.GetSockIPEndPoint();
            }

            // Don't initialize _frame until SocketInput and SocketOutput are set to their final values.
            if (ServerInformation.ConnectionFilter == null)
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
                    ServerInformation.ConnectionFilter.OnConnectionAsync(_filterContext).ContinueWith((task, state) =>
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
                        return _readFilteredInputTask;
                    case ConnectionState.CreatingFrame:
                        _connectionState = ConnectionState.ToDisconnect;
                        break;
                    case ConnectionState.Open:
                        _frame.Stop();
                        SocketInput.CompleteAwaiting();
                        break;
                }

                _socketClosedTcs = new TaskCompletionSource<object>();
                return Task.WhenAll(_socketClosedTcs.Task, _readFilteredInputTask);
            }
        }

        public virtual void Abort()
        {
            lock (_stateLock)
            {
                if (_connectionState == ConnectionState.CreatingFrame)
                {
                    _connectionState = ConnectionState.ToDisconnect;
                }
                else
                {
                    // Frame.Abort calls user code while this method is always
                    // called from a libuv thread.
                    System.Threading.ThreadPool.QueueUserWorkItem(state =>
                    {
                        var connection = (Connection)state;
                        connection._frame.Abort();
                    }, this);
                }
            }
        }

        // Called on Libuv thread
        public virtual void OnSocketClosed()
        {
            _rawSocketInput.Dispose();

            // If a connection filter was applied there will be two SocketInputs.
            // If a connection filter failed, SocketInput will be null.
            if (SocketInput != null && SocketInput != _rawSocketInput)
            {
                SocketInput.Dispose();
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

                if (_readFilteredInputTask.IsCompleted)
                {
                    ConnectionManager.ConnectionStopped(_connectionId);
                }
                else
                {
                    _readFilteredInputTask.ContinueWith((t, state) =>
                    {
                        var connection = (Connection)state;
                        connection.ConnectionManager.ConnectionStopped(connection._connectionId);
                    }, this);
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
                        var filteredStreamAdapter = new FilteredStreamAdapter(_filterContext.Connection, Memory2, Log, ThreadPool);

                        SocketInput = filteredStreamAdapter.SocketInput;
                        SocketOutput = filteredStreamAdapter.SocketOutput;

                        _readFilteredInputTask = filteredStreamAdapter.ReadInputAsync();
                    }
                    else
                    {
                        SocketInput = _rawSocketInput;
                        SocketOutput = _rawSocketOutput;
                    }

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
            var normalRead = status > 0;
            var normalDone = status == 0 || status == Constants.ECONNRESET || status == Constants.EOF;
            var errorDone = !(normalDone || normalRead);
            var readCount = normalRead ? status : 0;

            if (normalRead)
            {
                Log.ConnectionRead(_connectionId, readCount);
            }
            else
            {
                _socket.ReadStop();
                Log.ConnectionReadFin(_connectionId);
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
            return FrameFactory(this, _remoteEndPoint, _localEndPoint, _filterContext?.PrepareRequest);
        }

        void IConnectionControl.Pause()
        {
            Log.ConnectionPause(_connectionId);
            _socket.ReadStop();
        }

        void IConnectionControl.Resume()
        {
            Log.ConnectionResume(_connectionId);
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

                        Log.ConnectionKeepAlive(_connectionId);
                        break;
                    case ProduceEndType.SocketShutdown:
                    case ProduceEndType.SocketDisconnect:
                        if (_connectionState == ConnectionState.Disconnecting ||
                            _connectionState == ConnectionState.SocketClosed)
                        {
                            return;
                        }
                        _connectionState = ConnectionState.Disconnecting;

                        Log.ConnectionDisconnect(_connectionId);
                        _rawSocketOutput.End(endType);
                        break;
                }
            }
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
