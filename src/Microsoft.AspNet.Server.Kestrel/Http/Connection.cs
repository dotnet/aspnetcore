// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class Connection : ConnectionContext, IConnectionControl
    {
        private static readonly Action<UvStreamHandle, int, int, Exception, object> _readCallback = ReadCallback;
        private static readonly Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback = AllocCallback;

        private static long _lastConnectionId;

        private readonly UvStreamHandle _socket;
        private Frame _frame;
        private ConnectionFilterContext _filterContext;
        private readonly long _connectionId;

        private readonly SocketInput _rawSocketInput;
        private readonly SocketOutput _rawSocketOutput;

        private readonly object _stateLock = new object();
        private ConnectionState _connectionState;

        public Connection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            ConnectionControl = this;

            _connectionId = Interlocked.Increment(ref _lastConnectionId);

            _rawSocketInput = new SocketInput(Memory2);
            _rawSocketOutput = new SocketOutput(Thread, _socket, _connectionId, Log);
        }

        public void Start()
        {
            Log.ConnectionStart(_connectionId);

            // Start socket prior to applying the ConnectionFilter
            _socket.ReadStart(_allocCallback, _readCallback, this);

            // Don't initialize _frame until SocketInput and SocketOutput are set to their final values.
            if (ConnectionFilter == null)
            {
                SocketInput = _rawSocketInput;
                SocketOutput = _rawSocketOutput;

                _frame = new Frame(this);
                _frame.Start();
            }
            else
            {
                var libuvStream = new LibuvStream(_rawSocketInput, _rawSocketOutput);

                _filterContext = new ConnectionFilterContext
                {
                    Connection = libuvStream,
                    Address = ServerAddress
                };

                ConnectionFilter.OnConnection(_filterContext).ContinueWith((task, state) =>
                {
                    var connection = (Connection)state;

                    if (task.IsFaulted)
                    {
                        connection.Log.LogError("ConnectionFilter.OnConnection", task.Exception);
                        ConnectionControl.End(ProduceEndType.SocketDisconnect);
                    }
                    else if (task.IsCanceled)
                    {
                        connection.Log.LogError("ConnectionFilter.OnConnection Canceled");
                        ConnectionControl.End(ProduceEndType.SocketDisconnect);
                    }
                    else
                    {
                        connection.ApplyConnectionFilter();
                    }
                }, this);
            }
        }

        private void ApplyConnectionFilter()
        {
            var filteredStreamAdapter = new FilteredStreamAdapter(_filterContext.Connection, Memory2, Log);

            SocketInput = filteredStreamAdapter.SocketInput;
            SocketOutput = filteredStreamAdapter.SocketOutput;

            _frame = new Frame(this);
            _frame.Start();
        }

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((Connection)state).OnAlloc(handle, suggestedSize);
        }

        private Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            var result = _rawSocketInput.IncomingStart(2048);

            return handle.Libuv.buf_init(
                result.DataPtr,
                result.Data.Count);
        }

        private static void ReadCallback(UvStreamHandle handle, int readCount, int errorCode, Exception error, object state)
        {
            ((Connection)state).OnRead(handle, readCount, errorCode, error);
        }

        private void OnRead(UvStreamHandle handle, int readCount, int errorCode, Exception error)
        {
            var normalRead = readCount != 0 && errorCode == 0;
            var normalDone = readCount == 0 && (errorCode == 0 || errorCode == Constants.ECONNRESET || errorCode == Constants.EOF);
            var errorDone = !(normalDone || normalRead);

            if (normalRead)
            {
                Log.ConnectionRead(_connectionId, readCount);
            }
            else if (normalDone || errorDone)
            {
                _socket.ReadStop();
                Log.ConnectionReadFin(_connectionId);
            }

            _rawSocketInput.IncomingComplete(readCount, errorDone ? error : null);
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
                    case ProduceEndType.SocketShutdownSend:
                        if (_connectionState != ConnectionState.Open)
                        {
                            return;
                        }
                        _connectionState = ConnectionState.Shutdown;

                        Log.ConnectionWriteFin(_connectionId);
                        _rawSocketOutput.End(endType);
                        break;
                    case ProduceEndType.ConnectionKeepAlive:
                        if (_connectionState != ConnectionState.Open)
                        {
                            return;
                        }

                        Log.ConnectionKeepAlive(_connectionId);
                        break;
                    case ProduceEndType.SocketDisconnect:
                        if (_connectionState == ConnectionState.Disconnected)
                        {
                            return;
                        }
                        _connectionState = ConnectionState.Disconnected;

                        Log.ConnectionDisconnect(_connectionId);
                        _rawSocketOutput.End(endType);
                        break;
                }
            }
        }

        private enum ConnectionState
        {
            Open,
            Shutdown,
            Disconnected
        }
    }
}
