// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class SocketInput : ICriticalNotifyCompletion
    {
        private static readonly Action _awaitableIsCompleted = () => { };
        private static readonly Action _awaitableIsNotCompleted = () => { };

        private readonly MemoryPool2 _memory;

        private Action _awaitableState;
        private Exception _awaitableError;

        private MemoryPoolBlock2 _head;
        private MemoryPoolBlock2 _tail;
        private MemoryPoolBlock2 _pinned;
        private readonly object _syncHeadAndTail = new Object();

        public SocketInput(MemoryPool2 memory)
        {
            _memory = memory;
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

        public PinResult Pin(int minimumSize)
        {
            lock (_syncHeadAndTail)
            {
                if (_tail != null && minimumSize <= _tail.Data.Offset + _tail.Data.Count - _tail.End)
                {
                    _pinned = _tail;
                    var data = new ArraySegment<byte>(_pinned.Data.Array, _pinned.End, _pinned.Data.Offset + _pinned.Data.Count - _pinned.End);
                    var dataPtr = _pinned.Pin();
                    return new PinResult
                    {
                        Data = data,
                        DataPtr = dataPtr,
                    };
                }
            }

            _pinned = _memory.Lease(minimumSize);
            return new PinResult
            {
                Data = _pinned.Data,
                DataPtr = _pinned.Pin()
            };
        }

        public void Unpin(int count)
        {
            // Unpin may called without an earlier Pin 
            if (_pinned != null)
            {
                lock (_syncHeadAndTail)
                {
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
                Task.Run(continuation);
            }
            else
            {
                // THIS IS AN ERROR STATE - ONLY ONE WAITER CAN WAIT
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void SetCompleted(Exception error)
        {
            if (error != null)
            {
                _awaitableError = error;
            }

            var awaitableState = Interlocked.Exchange(
                ref _awaitableState,
                _awaitableIsCompleted);

            if (awaitableState != _awaitableIsCompleted &&
                awaitableState != _awaitableIsNotCompleted)
            {
                Task.Run(awaitableState);
            }
        }

        public void SetNotCompleted()
        {
            if (RemoteIntakeFin || _awaitableError != null)
            {
                // TODO: Race condition - setting either of these can leave awaitable not completed
                return;
            }
            var awaitableState = Interlocked.CompareExchange(
                ref _awaitableState,
                _awaitableIsNotCompleted,
                _awaitableIsCompleted);

            if (awaitableState == _awaitableIsNotCompleted)
            {
                return;
            }
            else if (awaitableState == _awaitableIsCompleted)
            {
                return;
            }
            else
            {
                // THIS IS AN ERROR STATE - ONLY ONE WAITER MAY EXIST
            }
        }

        public void GetResult()
        {
            var error = _awaitableError;
            if (error != null)
            {
                throw new AggregateException(error);
            }
        }

        public MemoryPoolBlock2.Iterator GetIterator()
        {
            lock (_syncHeadAndTail)
            {
                return new MemoryPoolBlock2.Iterator(_head);
            }
        }

        public void JumpTo(MemoryPoolBlock2.Iterator iterator)
        {
            MemoryPoolBlock2 returnStart;
            MemoryPoolBlock2 returnEnd;
            lock (_syncHeadAndTail)
            {
                // TODO: leave _pinned intact

                returnStart = _head;
                returnEnd = iterator.Block;
                _head = iterator.Block;
                if (_head == null)
                {
                    _tail = null;
                    SetNotCompleted();
                }
                else
                {
                    _head.Start = iterator.Index;
                }
            }
            while (returnStart != returnEnd)
            {
                var returnBlock = returnStart;
                returnStart = returnStart.Next;
                returnBlock.Pool.Return(returnBlock);
            }
        }

        public struct PinResult
        {
            public ArraySegment<byte> Data;
            public IntPtr DataPtr;
        }
    }
}
