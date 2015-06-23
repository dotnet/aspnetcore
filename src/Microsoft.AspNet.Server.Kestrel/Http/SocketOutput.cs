// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Threading;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class SocketOutput : ISocketOutput
    {
        private readonly KestrelThread _thread;
        private readonly UvStreamHandle _socket;

        public SocketOutput(KestrelThread thread, UvStreamHandle socket)
        {
            _thread = thread;
            _socket = socket;
        }

        public void Write(ArraySegment<byte> buffer, Action<Exception, object> callback, object state)
        {
            //TODO: need buffering that works
            var copy = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, copy, 0, buffer.Count);
            buffer = new ArraySegment<byte>(copy);

            KestrelTrace.Log.ConnectionWrite(0, buffer.Count);
            var req = new ThisWriteReq();
            req.Init(_thread.Loop);
            req.Contextualize(this, _socket, buffer, callback, state);
            req.Write();
        }

        public class ThisWriteReq : UvWriteReq
        {
            SocketOutput _self;
            ArraySegment<byte> _buffer;
            UvStreamHandle _socket;
            Action<Exception, object> _callback;
            object _state;
            Exception _callbackError;

            internal void Contextualize(
                SocketOutput socketOutput,
                UvStreamHandle socket,
                ArraySegment<byte> buffer,
                Action<Exception, object> callback,
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
                _self._thread.Post(obj =>
                {
                    var req = (ThisWriteReq)obj;
                    req.Write(
                        req._socket,
                        new ArraySegment<ArraySegment<byte>>(
                            new[] { req._buffer }),
                        (r, status, error, state) => ((ThisWriteReq)state).OnWrite(status, error),
                        req);
                }, this);
            }

            private void OnWrite(int status, Exception error)
            {
                KestrelTrace.Log.ConnectionWriteCallback(0, status);
                //NOTE: pool this?

                Dispose();

                // Get off the event loop before calling user code!
                _callbackError = error;
                ThreadPool.QueueUserWorkItem(obj =>
                {
                    var req = (ThisWriteReq)obj;
                    req._callback(req._callbackError, req._state);
                }, this);
           }
        }


        public bool Flush(Action drained)
        {
            return false;
        }

    }
}
