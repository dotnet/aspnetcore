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
            Action<Exception, object, bool> callback,
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
                // callback(error, state, calledInline)
                callback(null, state, true);
            }
        }

        public void End(ProduceEndType endType)
        {
            switch (endType)
            {
                case ProduceEndType.SocketShutdownSend:
                    Write(default(ArraySegment<byte>), (error, state, calledInline) => { }, null,
                        immediate: true,
                        socketShutdownSend: true,
                        socketDisconnect: false);
                    break;
                case ProduceEndType.SocketDisconnect:
                    Write(default(ArraySegment<byte>), (error, state, calledInline) => { }, null,
                        immediate: true,
                        socketShutdownSend: false,
                        socketDisconnect: true);
                    break;
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
                writingContext.DoWriteIfNeeded();
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

                    // callback(error, state, calledInline)
                    callbackContext.Callback(_lastWriteError, callbackContext.State, false);
                }

                // Now that the while loop has completed the following invariants should hold true:
                Debug.Assert(_numBytesPreCompleted >= 0);
                Debug.Assert(_numBytesPreCompleted <= _maxBytesPreCompleted);
            }
        }

        void ISocketOutput.Write(ArraySegment<byte> buffer, bool immediate)
        {
            if (!immediate)
            {
                // immediate==false calls always return complete tasks, because there is guaranteed
                // to be a subsequent immediate==true call which will go down the following code-path
                Write(
                    buffer,
                    (error, state, calledInline) => { },
                    null,
                    immediate: false);
                return;
            }

            // TODO: Optimize task being used, and remove callback model from the underlying Write
            var tcs = new TaskCompletionSource<int>();

            Write(
                buffer,
                (error, state, calledInline) =>
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
                immediate: true);

            if (tcs.Task.Status != TaskStatus.RanToCompletion)
            {
                tcs.Task.GetAwaiter().GetResult();
            }
        }

        Task ISocketOutput.WriteAsync(ArraySegment<byte> buffer, bool immediate, CancellationToken cancellationToken)
        {
            if (!immediate)
            {
                // immediate==false calls always return complete tasks, because there is guaranteed
                // to be a subsequent immediate==true call which will go down the following code-path
                Write(
                    buffer,
                    (error, state, calledInline) => { },
                    null,
                    immediate: false);
                return TaskUtilities.CompletedTask;
            }

            // TODO: Optimize task being used, and remove callback model from the underlying Write
            var tcs = new TaskCompletionSource<int>();

            Write(
                buffer,
                (error, state, calledInline) =>
                {
                    if (!calledInline)
                    {
                        ThreadPool.QueueUserWorkItem(state2 =>
                        {
                            var tcs2 = (TaskCompletionSource<int>)state2;
                            if (error != null)
                            {
                                tcs2.SetException(error);
                            }
                            else
                            {
                                tcs2.SetResult(0);
                            }
                        }, state);
                    }
                    else
                    {
                        var tcs2 = (TaskCompletionSource<int>)state;
                        if (error != null)
                        {
                            tcs2.SetException(error);
                        }
                        else
                        {
                            tcs2.SetResult(0);
                        }
                    }
                },
                tcs,
                immediate: true);

            return tcs.Task;
        }

        private class CallbackContext
        {
            // callback(error, state, calledInline)
            public Action<Exception, object, bool> Callback;
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

            /// <summary>
            /// Perform any actions needed by this work item. The individual tasks are non-blocking and
            /// will continue through to each other in order.
            /// </summary>
            public void Execute()
            {
                DoWriteIfNeeded();
            }

            /// <summary>
            /// First step: initiate async write if needed, otherwise go to next step
            /// </summary>
            public void DoWriteIfNeeded()
            {
                if (Buffers.Count == 0 || Self._socket.IsClosed)
                {
                    DoShutdownIfNeeded();
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
                    DoShutdownIfNeeded();
                }, this);
            }

            /// <summary>
            /// Second step: initiate async shutdown if needed, otherwise go to next step
            /// </summary>
            public void DoShutdownIfNeeded()
            {
                if (SocketShutdownSend == false || Self._socket.IsClosed)
                {
                    DoDisconnectIfNeeded();
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

                    DoDisconnectIfNeeded();
                }, this);
            }

            /// <summary>
            /// Third step: disconnect socket if needed, otherwise this work item is complete
            /// </summary>
            public void DoDisconnectIfNeeded()
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
