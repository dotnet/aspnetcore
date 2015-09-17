// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class Connection : ConnectionContext, IConnectionControl
    {
        private const int EOF = -4095;
        private const int ECONNRESET = -4077;

        private static readonly Action<UvStreamHandle, int, int, Exception, object> _readCallback = ReadCallback;
        private static readonly Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback = AllocCallback;

        private static long _lastConnectionId;

        private readonly UvStreamHandle _socket;
        private Frame _frame;
        private Task _frameTask;
        private long _connectionId = 0;

        private readonly object _stateLock = new object();
        private ConnectionState _connectionState;

        public Connection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            ConnectionControl = this;

            _connectionId = Interlocked.Increment(ref _lastConnectionId);
        }

        public void Start()
        {
            Log.ConnectionStart(_connectionId);

            SocketInput = new SocketInput(Memory2);
            SocketOutput = new SocketOutput(Thread, _socket, _connectionId, Log);
            _frame = new Frame(this);
            _frameTask = Task.Run(_frame.ProcessFraming);
            _socket.ReadStart(_allocCallback, _readCallback, this);
        }

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((Connection)state).OnAlloc(handle, suggestedSize);
        }

        private Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            var result = SocketInput.Pin(2048);

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
            SocketInput.Unpin(readCount);

            var normalRead = readCount != 0 && errorCode == 0;
            var normalDone = readCount == 0 && (errorCode == 0 || errorCode == Constants.ECONNRESET || errorCode == Constants.EOF);
            var errorDone = !(normalDone || normalRead);

            if (normalRead)
            {
                Log.ConnectionRead(_connectionId, readCount);
            }
            else if (normalDone || errorDone)
            {
                SocketInput.RemoteIntakeFin = true;
                _socket.ReadStop();
                Log.ConnectionReadFin(_connectionId);
            }

            SocketInput.SetCompleted(errorDone ? error : null);
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
                        SocketOutput.End(endType);
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
                        SocketOutput.End(endType);
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
