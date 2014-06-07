// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    ///   Operations performed for buffered socket output
    /// </summary>
    public interface ISocketOutput
    {
        void Write(ArraySegment<byte> buffer, Action<object> callback, object state);
    }

    public class SocketOutput : ISocketOutput
    {
        private readonly KestrelThread _thread;
        private readonly UvStreamHandle _socket;

        public SocketOutput(KestrelThread thread, UvStreamHandle socket)
        {
            _thread = thread;
            _socket = socket;
        }

        public void Write(ArraySegment<byte> buffer, Action<object> callback, object state)
        {
            var req = new ThisWriteReq();
            req.Init(_thread.Loop);
            req.Contextualize(this, _socket, buffer, callback, state);
            _thread.Post(x =>
            {
                ((ThisWriteReq)x).Write();
            }, req);
        }

        public class ThisWriteReq : UvWriteReq
        {
            private static readonly Action<UvWriteReq, int, object> _writeCallback = WriteCallback;
            private static void WriteCallback(UvWriteReq req, int status, object state)
            {
                ((ThisWriteReq)state).OnWrite(req, status);
            }

            SocketOutput _self;
            ArraySegment<byte> _buffer;
            Action<Exception> _drained;
            UvStreamHandle _socket;
            Action<object> _callback;
            object _state;
            GCHandle _pin;

            internal void Contextualize(
                SocketOutput socketOutput,
                UvStreamHandle socket,
                ArraySegment<byte> buffer,
                Action<object> callback,
                object state)
            {
                _self = socketOutput;
                _socket = socket;
                _buffer = buffer;
                _callback = callback;
                _state = state;
            }

            public void Write()
            {
                _pin = GCHandle.Alloc(_buffer.Array, GCHandleType.Pinned);
                var buf = new Libuv.uv_buf_t
                {
                    len = (uint)_buffer.Count,
                    memory = _pin.AddrOfPinnedObject() + _buffer.Offset
                };

                Write(
                    _socket,
                    new[] { buf },
                    1,
                    _writeCallback,
                    this);
            }

            private void OnWrite(UvWriteReq req, int status)
            {
                _pin.Free();
                //NOTE: pool this?
                Close();
                _callback(_state);
            }
        }


        public bool Flush(Action drained)
        {
            return false;
        }

    }
}
