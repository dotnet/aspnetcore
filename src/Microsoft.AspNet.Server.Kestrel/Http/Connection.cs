// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Networking;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class ConnectionContext : ListenerContext
    {
        public ConnectionContext()
        {
        }

        public ConnectionContext(ListenerContext context) : base(context)
        {
        }

        public SocketInput SocketInput { get; set; }
        public ISocketOutput SocketOutput { get; set; }

        public IConnectionControl ConnectionControl { get; set; }
    }

    public interface IConnectionControl
    {
        void Pause();
        void Resume();
        void End(ProduceEndType endType);
    }

    public class Connection : ConnectionContext, IConnectionControl
    {
        private static readonly Action<UvStreamHandle, int, object> _readCallback = ReadCallback;
        private static readonly Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback = AllocCallback;

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((Connection)state).OnAlloc(handle, suggestedSize);
        }

        private static void ReadCallback(UvStreamHandle handle, int nread, object state)
        {
            ((Connection)state).OnRead(handle, nread);
        }


        private readonly Func<object, Task> _app;
        private readonly UvStreamHandle _socket;

        private Frame _frame;

        private Action<Exception> _fault;
        private Action<Frame, Exception> _frameConsumeCallback;
        private Action _receiveAsyncCompleted;
        private Frame _receiveAsyncCompletedFrame;

        public Connection(ListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            ConnectionControl = this;
        }

        public void Start()
        {
            //_services.Trace.Event(TraceEventType.Start, TraceMessage.Connection);

            SocketInput = new SocketInput(Memory);
            SocketOutput = new SocketOutput(Thread, _socket);

            _socket.ReadStart(_allocCallback, _readCallback, this);

            //_fault = ex => { Debug.WriteLine(ex.Message); };

            //_frameConsumeCallback = (frame, error) =>
            //{
            //    if (error != null)
            //    {
            //        _fault(error);
            //    }
            //    try
            //    {
            //        Go(false, frame);
            //    }
            //    catch (Exception ex)
            //    {
            //        _fault(ex);
            //    }
            //};

            //try
            //{
            //    //_socket.Blocking = false;
            //    //_socket.NoDelay = true;
            //    Go(true, null);
            //}
            //catch (Exception ex)
            //{
            //    _fault(ex);
            //}
        }

        private Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            return new Libuv.uv_buf_t
            {
                memory = SocketInput.Pin(2048),
                len = 2048
            };
        }

        private void OnRead(UvStreamHandle handle, int nread)
        {
            SocketInput.Unpin(nread);

            if (nread == 0)
            {
                SocketInput.RemoteIntakeFin = true;
            }

            if (_frame == null)
            {
                _frame = new Frame(this);
            }
            _frame.Consume();
        }

        void IConnectionControl.Pause()
        {
            _socket.ReadStop();
        }

        void IConnectionControl.Resume()
        {
            _socket.ReadStart(_allocCallback, _readCallback, this);
        }

        void IConnectionControl.End(ProduceEndType endType)
        {
            switch (endType)
            {
                case ProduceEndType.SocketShutdownSend:
                    var shutdown = new UvShutdownReq();
                    shutdown.Init(Thread.Loop);
                    shutdown.Shutdown(_socket, (req, status, state) => req.Close(), null);
                    break;
                case ProduceEndType.ConnectionKeepAlive:
                    break;
                case ProduceEndType.SocketDisconnect:
                    _socket.Close();
                    break;
            }
        }
    }
}
