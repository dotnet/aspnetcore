// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class SocketOutput : ISocketOutput
    {
        private const int _maxPendingWrites = 3;
        private const int _maxBytesPreCompleted = 65536;

        private readonly KestrelThread _thread;
        private readonly UvStreamHandle _socket;
        private readonly long _connectionId;
        private readonly IKestrelTrace _log;

        // This locks access to to all of the below fields
        private readonly object _lockObj = new object();

        // The number of write operations that have been scheduled so far
        // but have not completed.
        private int _writesPending = 0;

        private int _numBytesPreCompleted = 0;
        private Exception _lastWriteError;
        private WriteContext _nextWriteContext;
        private readonly Queue<CallbackContext> _callbacksPending;

        public SocketOutput(KestrelThread thread, UvStreamHandle socket, long connectionId, IKestrelTrace log)
        {
            _thread = thread;
            _socket = socket;
            _connectionId = connectionId;
            _log = log;
            _callbacksPending = new Queue<CallbackContext>();
        }

        public void Write(
            ArraySegment<byte> buffer,
            Action<Exception, object> callback,
            object state,
            bool immediate = true,
            bool socketShutdownSend = false,
            bool socketDisconnect = false)
        {
            //TODO: need buffering that works
            if (buffer.Array != null)
            {
                var copy = new byte[buffer.Count];
                Array.Copy(buffer.Array, buffer.Offset, copy, 0, buffer.Count);
                buffer = new ArraySegment<byte>(copy);
                _log.ConnectionWrite(_connectionId, buffer.Count);
            }

            bool triggerCallbackNow = false;

            lock (_lockObj)
            {
                if (_nextWriteContext == null)
                {
                    _nextWriteContext = new WriteContext(this);
                }

                if (buffer.Array != null)
                {
                    _nextWriteContext.Buffers.Enqueue(buffer);
                }
                if (socketShutdownSend)
                {
                    _nextWriteContext.SocketShutdownSend = true;
                }
                if (socketDisconnect)
                {
                    _nextWriteContext.SocketDisconnect = true;
                }
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
            _thread.Post(_this => _this.WriteAllPending(), this);
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
                writingContext.Execute();
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
        private void OnWriteCompleted(Queue<ArraySegment<byte>> writtenBuffers, int status, Exception error)
        {
            _log.ConnectionWriteCallback(_connectionId, status);

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

        void ISocketOutput.Write(ArraySegment<byte> buffer, bool immediate)
        {
            ((ISocketOutput)this).WriteAsync(buffer, immediate).Wait();
        }

        Task ISocketOutput.WriteAsync(ArraySegment<byte> buffer, bool immediate, CancellationToken cancellationToken)
        {
            // TODO: Optimize task being used, and remove callback model from the underlying Write
            var tcs = new TaskCompletionSource<int>();

            Write(
                buffer,
                (error, state) =>
                {
                    if (error != null)
                    {
                        tcs.SetException(error);
                    }
                    else
                    {
                        tcs.SetResult(0);
                    }
                },
                tcs,
                immediate: immediate);

            return tcs.Task;
        }

        void ISocketOutput.End(ProduceEndType endType)
        {
            switch (endType)
            {
                case ProduceEndType.SocketShutdownSend:
                    Write(default(ArraySegment<byte>), (error, state) => { }, null,
                        immediate: true,
                        socketShutdownSend: true,
                        socketDisconnect: false);
                    break;
                case ProduceEndType.SocketDisconnect:
                    Write(default(ArraySegment<byte>), (error, state) => { }, null,
                        immediate: true,
                        socketShutdownSend: false,
                        socketDisconnect: true);
                    break;
            }
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
            public SocketOutput Self;

            public Queue<ArraySegment<byte>> Buffers;
            public bool SocketShutdownSend;
            public bool SocketDisconnect;

            public int WriteStatus;
            public Exception WriteError;

            public int ShutdownSendStatus;

            public WriteContext(SocketOutput self)
            {
                Self = self;
                Buffers = new Queue<ArraySegment<byte>>();
            }

            public void Execute()
            {
                if (Buffers.Count == 0 || Self._socket.IsClosed)
                {
                    StageTwo();
                    return;
                }

                var buffers = new ArraySegment<byte>[Buffers.Count];

                var i = 0;
                foreach (var buffer in Buffers)
                {
                    buffers[i++] = buffer;
                }

                var writeReq = new UvWriteReq(Self._log);
                writeReq.Init(Self._thread.Loop);
                writeReq.Write(Self._socket, new ArraySegment<ArraySegment<byte>>(buffers), (_writeReq, status, error, state) =>
                {
                    _writeReq.Dispose();
                    var _this = (WriteContext)state;
                    _this.WriteStatus = status;
                    _this.WriteError = error;
                    StageTwo();
                }, this);
            }

            public void StageTwo()
            {
                if (SocketShutdownSend == false || Self._socket.IsClosed)
                {
                    StageThree();
                    return;
                }

                var shutdownReq = new UvShutdownReq(Self._log);
                shutdownReq.Init(Self._thread.Loop);
                shutdownReq.Shutdown(Self._socket, (_shutdownReq, status, state) =>
                {
                    _shutdownReq.Dispose();
                    var _this = (WriteContext)state;
                    _this.ShutdownSendStatus = status;

                    Self._log.ConnectionWroteFin(Self._connectionId, status);

                    StageThree();
                }, this);
            }

            public void StageThree()
            {
                if (SocketDisconnect == false || Self._socket.IsClosed)
                {
                    Complete();
                    return;
                }

                Self._socket.Dispose();
                Self._log.ConnectionStop(Self._connectionId);
                Complete();
            }

            public void Complete()
            {
                Self.OnWriteCompleted(Buffers, WriteStatus, WriteError);
            }
        }
    }
}
