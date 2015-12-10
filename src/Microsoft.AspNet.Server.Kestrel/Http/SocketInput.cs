// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class SocketInput : ICriticalNotifyCompletion
    {
        private static readonly Action _awaitableIsCompleted = () => { };
        private static readonly Action _awaitableIsNotCompleted = () => { };

        private readonly MemoryPool2 _memory;
        private readonly IThreadPool _threadPool;
        private readonly ManualResetEventSlim _manualResetEvent = new ManualResetEventSlim(false);

        private Action _awaitableState;
        private Exception _awaitableError;

        private MemoryPoolBlock2 _head;
        private MemoryPoolBlock2 _tail;
        private MemoryPoolBlock2 _pinned;
        private readonly object _sync = new Object();

        public SocketInput(MemoryPool2 memory, IThreadPool threadPool)
        {
            _memory = memory;
            _threadPool = threadPool;
            _awaitableState = _awaitableIsNotCompleted;
        }

        public ArraySegment<byte> Buffer { get; set; }

        public bool RemoteIntakeFin { get; set; }

        public bool IsCompleted
        {
            get
            {
                return Equals(_awaitableState, _awaitableIsCompleted);
            }
        }

        public void Skip(int count)
        {
            Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset + count, Buffer.Count - count);
        }

        public ArraySegment<byte> Take(int count)
        {
            var taken = new ArraySegment<byte>(Buffer.Array, Buffer.Offset, count);
            Skip(count);
            return taken;
        }

        public IncomingBuffer IncomingStart(int minimumSize)
        {
            lock (_sync)
            {
                if (_tail != null && minimumSize <= _tail.Data.Offset + _tail.Data.Count - _tail.End)
                {
                    _pinned = _tail;
                    var data = new ArraySegment<byte>(_pinned.Data.Array, _pinned.End, _pinned.Data.Offset + _pinned.Data.Count - _pinned.End);
                    var dataPtr = _pinned.Pin() + _pinned.End;
                    return new IncomingBuffer
                    {
                        Data = data,
                        DataPtr = dataPtr,
                    };
                }
            }

            _pinned = _memory.Lease(minimumSize);
            return new IncomingBuffer
            {
                Data = _pinned.Data,
                DataPtr = _pinned.Pin() + _pinned.End
            };
        }

        public void IncomingComplete(int count, Exception error)
        {
            Action awaitableState;

            lock (_sync)
            {
                // Unpin may called without an earlier Pin 
                if (_pinned != null)
                {
                    _pinned.Unpin();

                    _pinned.End += count;
                    if (_head == null)
                    {
                        _head = _tail = _pinned;
                    }
                    else if (_tail == _pinned)
                    {
                        // NO-OP: this was a read into unoccupied tail-space
                    }
                    else
                    {
                        _tail.Next = _pinned;
                        _tail = _pinned;
                    }
                }
                _pinned = null;

                if (count == 0)
                {
                    RemoteIntakeFin = true;
                }
                if (error != null)
                {
                    _awaitableError = error;
                }

                awaitableState = Interlocked.Exchange(
                    ref _awaitableState,
                    _awaitableIsCompleted);

                _manualResetEvent.Set();
            }

            if (awaitableState != _awaitableIsCompleted &&
                awaitableState != _awaitableIsNotCompleted)
            {
                _threadPool.Run(awaitableState);
            }
        }

        public MemoryPoolIterator2 ConsumingStart()
        {
            lock (_sync)
            {
                return new MemoryPoolIterator2(_head);
            }
        }

        public void ConsumingComplete(
            MemoryPoolIterator2 consumed,
            MemoryPoolIterator2 examined)
        {
            MemoryPoolBlock2 returnStart = null;
            MemoryPoolBlock2 returnEnd = null;
            lock (_sync)
            {
                if (!consumed.IsDefault)
                {
                    returnStart = _head;
                    returnEnd = consumed.Block;
                    _head = consumed.Block;
                    _head.Start = consumed.Index;
                }
                if (!examined.IsDefault &&
                    examined.IsEnd &&
                    RemoteIntakeFin == false &&
                    _awaitableError == null)
                {
                    _manualResetEvent.Reset();

                    var awaitableState = Interlocked.CompareExchange(
                        ref _awaitableState,
                        _awaitableIsNotCompleted,
                        _awaitableIsCompleted);
                }
            }
            while (returnStart != returnEnd)
            {
                var returnBlock = returnStart;
                returnStart = returnStart.Next;
                returnBlock.Pool?.Return(returnBlock);
            }
        }

        public void AbortAwaiting()
        {
            _awaitableError = new ObjectDisposedException(nameof(SocketInput), "The request was aborted");

            var awaitableState = Interlocked.Exchange(
                ref _awaitableState,
                _awaitableIsCompleted);

            _manualResetEvent.Set();

            if (awaitableState != _awaitableIsCompleted &&
                awaitableState != _awaitableIsNotCompleted)
            {
                _threadPool.Run(awaitableState);
            }
        }

        public SocketInput GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            var awaitableState = Interlocked.CompareExchange(
                ref _awaitableState,
                continuation,
                _awaitableIsNotCompleted);

            if (awaitableState == _awaitableIsNotCompleted)
            {
                return;
            }
            else if (awaitableState == _awaitableIsCompleted)
            {
                _threadPool.Run(continuation);
            }
            else
            {
                _awaitableError = new InvalidOperationException("Concurrent reads are not supported.");

                awaitableState = Interlocked.Exchange(
                    ref _awaitableState,
                    _awaitableIsCompleted);

                _manualResetEvent.Set();

                _threadPool.Run(continuation);
                _threadPool.Run(awaitableState);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void GetResult()
        {
            if (!IsCompleted)
            {
                _manualResetEvent.Wait();
            }
            var error = _awaitableError;
            if (error != null)
            {
                throw new IOException(error.Message, error);
            }
        }

        public struct IncomingBuffer
        {
            public ArraySegment<byte> Data;
            public IntPtr DataPtr;
        }
    }
}
