// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNet.Server.Kestrel.Networking;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class SocketOutput : ISocketOutput
    {
        private const int _maxPendingWrites = 3;
        private const int _maxBytesPreCompleted = 65536;

        private readonly KestrelThread _thread;
        private readonly UvStreamHandle _socket;

        // This locks access to to all of the below fields
        private readonly object _lockObj = new object();

        // The number of write operations that have been scheduled so far
        // but have not completed.
        private int _writesPending = 0;

        private int _numBytesPreCompleted = 0;
        private Exception _lastWriteError;
        private WriteContext _nextWriteContext;
        private readonly Queue<CallbackContext> _callbacksPending;

        public SocketOutput(KestrelThread thread, UvStreamHandle socket)
        {
            _thread = thread;
            _socket = socket;
            _callbacksPending = new Queue<CallbackContext>();
        }

        public void Write(ArraySegment<byte> buffer, Action<Exception, object> callback, object state, bool immediate)
        {
            //TODO: need buffering that works
            var copy = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, copy, 0, buffer.Count);
            buffer = new ArraySegment<byte>(copy);

            KestrelTrace.Log.ConnectionWrite(0, buffer.Count);

            bool triggerCallbackNow = false;

            lock (_lockObj)
            {
                if (_nextWriteContext == null)
                {
                    _nextWriteContext = new WriteContext(this);
                }

                _nextWriteContext.Buffers.Enqueue(buffer);

                // Complete the write task immediately if all previous write tasks have been completed,
                // the buffers haven't grown too large, and the last write to the socket succeeded.
                triggerCallbackNow = _lastWriteError == null &&
                                     _callbacksPending.Count == 0 &&
                                     _numBytesPreCompleted + buffer.Count <= _maxBytesPreCompleted;
                if (triggerCallbackNow)
                {
                    _numBytesPreCompleted += buffer.Count;
                }
                else
                {
                    _callbacksPending.Enqueue(new CallbackContext
                    {
                        Callback = callback,
                        State = state,
                        BytesToWrite = buffer.Count
                    });
                }

                if (_writesPending < _maxPendingWrites && immediate)
                {
                    ScheduleWrite();
                    _writesPending++;
                }
            }

            // Make sure we call user code outside of the lock.
            if (triggerCallbackNow)
            {
                callback(null, state);
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
                    _writesPending--;
                    return;
                }
            }

            try
            {
                var buffers = new ArraySegment<byte>[writingContext.Buffers.Count];

                var i = 0;
                foreach (var buffer in writingContext.Buffers)
                {
                    buffers[i++] = buffer;
                }

                var writeReq = new UvWriteReq();
                writeReq.Init(_thread.Loop);

                writeReq.Write(_socket, new ArraySegment<ArraySegment<byte>>(buffers), (r, status, error, state) =>
                {
                    var writtenContext = (WriteContext)state;
                    writtenContext.Self.OnWriteCompleted(writtenContext.Buffers, r, status, error);
                }, writingContext);
            }
            catch
            {
                lock (_lockObj)
                {
                    // Lock instead of using Interlocked.Decrement so _writesSending
                    // doesn't change in the middle of executing other synchronized code.
                    _writesPending--;
                }

                throw;
            }
        }

        // This is called on the libuv event loop
        private void OnWriteCompleted(Queue<ArraySegment<byte>> writtenBuffers, UvWriteReq req, int status, Exception error)
        {
            KestrelTrace.Log.ConnectionWriteCallback(0, status);

            lock (_lockObj)
            {
                _lastWriteError = error;

                if (_nextWriteContext != null)
                {
                    ScheduleWrite();
                }
                else
                {
                    _writesPending--;
                }

                foreach (var writeBuffer in writtenBuffers)
                {
                    // _numBytesPreCompleted can temporarily go negative in the event there are
                    // completed writes that we haven't triggered callbacks for yet.
                    _numBytesPreCompleted -= writeBuffer.Count;
                }


                // bytesLeftToBuffer can be greater than _maxBytesPreCompleted
                // This allows large writes to complete once they've actually finished.
                var bytesLeftToBuffer = _maxBytesPreCompleted - _numBytesPreCompleted;
                while (_callbacksPending.Count > 0 &&
                       _callbacksPending.Peek().BytesToWrite <= bytesLeftToBuffer)
                {
                    var callbackContext = _callbacksPending.Dequeue();

                    _numBytesPreCompleted += callbackContext.BytesToWrite;

                    TriggerCallback(callbackContext);
                }

                // Now that the while loop has completed the following invariants should hold true:
                Trace.Assert(_numBytesPreCompleted >= 0);
                Trace.Assert(_numBytesPreCompleted <= _maxBytesPreCompleted);
            }

            req.Dispose();
        }

        private void TriggerCallback(CallbackContext context)
        {
            context.Error = _lastWriteError;
            ThreadPool.QueueUserWorkItem(obj =>
            {
                var c = (CallbackContext)obj;
                c.Callback(c.Error, c.State);
            }, context);
        }

        private class CallbackContext
        {
            public Exception Error;
            public Action<Exception, object> Callback;
            public object State;
            public int BytesToWrite;
        }

        private class WriteContext
        {
            public WriteContext(SocketOutput self)
            {
                Self = self;
                Buffers = new Queue<ArraySegment<byte>>();
            }

            public SocketOutput Self;
            public Queue<ArraySegment<byte>> Buffers;
        }
    }
}
