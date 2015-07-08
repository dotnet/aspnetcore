// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class SocketOutput : ISocketOutput
    {
        private const int _maxPendingWrites = 3;

        private readonly KestrelThread _thread;
        private readonly UvStreamHandle _socket;

        private WriteContext _nextWriteContext;

        // The number of write operations that have been scheduled so far
        // but have not completed.
        private int _writesSending = 0;

        // This locks all access to _nextWriteContext and _writesSending
        private readonly object _lockObj = new object();

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

            var context = new WriteOperation
            {
                Buffer = buffer,
                Callback = callback,
                State = state
            };

            lock (_lockObj)
            {
                if (_nextWriteContext == null)
                {
                    _nextWriteContext = new WriteContext(this);
                }

                _nextWriteContext.Operations.Add(context);

                if (_writesSending < _maxPendingWrites)
                {
                    ScheduleWrite();
                    _writesSending++;
                }
            }
        }

        private void ScheduleWrite()
        {
            _thread.Post(obj =>
            {
                var self = (SocketOutput)obj;
                self.WriteAllPending();
            }, this);
        }

        // This is called on the libuv event loop
        private void WriteAllPending()
        {
            WriteContext writingContext;

            lock (_lockObj)
            {
                if (_nextWriteContext != null)
                {
                    writingContext = _nextWriteContext;
                    _nextWriteContext = null;
                }
                else
                {
                    _writesSending--;
                    return;
                }
            }

            try
            {
                var buffers = new ArraySegment<byte>[writingContext.Operations.Count];

                var i = 0;
                foreach (var writeOp in writingContext.Operations)
                {
                    buffers[i] = writeOp.Buffer;
                    i++;
                }

                writingContext.WriteReq.Write(_socket, new ArraySegment<ArraySegment<byte>>(buffers), (r, status, error, state) =>
                {
                    var writtenContext = (WriteContext)state;
                    writtenContext.Self.OnWriteCompleted(writtenContext.Operations, r, status, error);
                }, writingContext);
            }
            catch
            {
                lock (_lockObj)
                {
                    // Lock instead of using Interlocked.Decrement so _writesSending
                    // doesn't change in the middle of executing other synchronized code.
                    _writesSending--;
                }

                throw;
            }
        }

        // This is called on the libuv event loop
        private void OnWriteCompleted(List<WriteOperation> completedWrites, UvWriteReq req, int status, Exception error)
        {
            lock (_lockObj)
            {
                if (_nextWriteContext != null)
                {
                    ScheduleWrite();
                }
                else
                {
                    _writesSending--;
                }
            }

            req.Dispose();

            foreach (var writeOp in completedWrites)
            {
                KestrelTrace.Log.ConnectionWriteCallback(0, status);
                //NOTE: pool this?

                // Get off the event loop before calling user code!
                writeOp.Error = error;
                ThreadPool.QueueUserWorkItem(obj =>
                {
                    var op = (WriteOperation)obj;
                    op.Callback(op.Error, op.State);
                }, writeOp);
            }
        }

        private class WriteOperation
        {
            public ArraySegment<byte> Buffer;
            public Exception Error;
            public Action<Exception, object> Callback;
            public object State;
        }

        private class WriteContext
        {
            public WriteContext(SocketOutput self)
            {
                Self = self;

                WriteReq = new UvWriteReq();
                WriteReq.Init(self._thread.Loop);

                Operations = new List<WriteOperation>();
            }

            public SocketOutput Self;

            public UvWriteReq WriteReq;
            public List<WriteOperation> Operations;
        }
    }
}
