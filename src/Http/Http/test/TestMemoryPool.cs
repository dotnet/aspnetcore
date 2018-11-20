// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class TestMemoryPool : MemoryPool<byte>
    {
        private MemoryPool<byte> _pool = Shared;

        private bool _disposed;

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            CheckDisposed();
            return new PooledMemory(_pool.Rent(minBufferSize), this);
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }

        public override int MaxBufferSize => 4096;

        internal void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TestMemoryPool));
            }
        }

        private class PooledMemory : MemoryManager<byte>
        {
            private IMemoryOwner<byte> _owner;

            private readonly TestMemoryPool _pool;

            private int _referenceCount;

            private bool _returned;

            private string _leaser;

            public PooledMemory(IMemoryOwner<byte> owner, TestMemoryPool pool)
            {
                _owner = owner;
                _pool = pool;
                _leaser = Environment.StackTrace;
                _referenceCount = 1;
            }

            ~PooledMemory()
            {
                Debug.Assert(_returned, "Block being garbage collected instead of returned to pool" + Environment.NewLine + _leaser);
            }

            protected override void Dispose(bool disposing)
            {
                _pool.CheckDisposed();
            }

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                _pool.CheckDisposed();
                Interlocked.Increment(ref _referenceCount);

                if (!MemoryMarshal.TryGetArray(_owner.Memory, out ArraySegment<byte> segment))
                {
                    throw new InvalidOperationException();
                }

                unsafe
                {
                    try
                    {
                        if ((uint)elementIndex > (uint)segment.Count)
                        {
                            throw new ArgumentOutOfRangeException(nameof(elementIndex));
                        }

                        GCHandle handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);

                        return new MemoryHandle(Unsafe.Add<byte>(((void*)handle.AddrOfPinnedObject()), elementIndex + segment.Offset), handle, this);
                    }
                    catch
                    {
                        Unpin();
                        throw;
                    }
                }
            }

            public override void Unpin()
            {
                _pool.CheckDisposed();

                int newRefCount = Interlocked.Decrement(ref _referenceCount);

                if (newRefCount < 0)
                    throw new InvalidOperationException();

                if (newRefCount == 0)
                {
                    _returned = true;
                }
            }

            protected override bool TryGetArray(out ArraySegment<byte> segment)
            {
                _pool.CheckDisposed();
                return MemoryMarshal.TryGetArray(_owner.Memory, out segment);
            }

            public override Memory<byte> Memory
            {
                get
                {
                    _pool.CheckDisposed();
                    return _owner.Memory;
                }
            }

            public override Span<byte> GetSpan()
            {
                _pool.CheckDisposed();
                return _owner.Memory.Span;
            }
        }
    }
}