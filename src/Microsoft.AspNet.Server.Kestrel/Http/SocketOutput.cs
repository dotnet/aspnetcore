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
            //TODO: need buffering that works
            var copy = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, copy, 0, buffer.Count);
            buffer = new ArraySegment<byte>(copy);

            KestrelTrace.Log.ConnectionWrite(0, buffer.Count);
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
                Write(
                    _socket,
                    new ArraySegment<ArraySegment<byte>>(
                        new[]{_buffer}),
                    _writeCallback,
                    this);
            }

            private void OnWrite(UvWriteReq req, int status)
            {
                KestrelTrace.Log.ConnectionWriteCallback(0, status);
                //NOTE: pool this?
                Dispose();
                _callback(_state);
            }
        }


        public bool Flush(Action drained)
        {
            return false;
        }

    }
}
