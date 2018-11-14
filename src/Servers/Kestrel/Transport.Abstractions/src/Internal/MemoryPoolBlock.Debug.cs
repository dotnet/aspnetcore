// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG

using System.Threading;
using System.Diagnostics;

namespace System.Buffers
{
    /// <summary>
    /// Block tracking object used by the byte buffer memory pool. A slab is a large allocation which is divided into smaller blocks. The
    /// individual blocks are then treated as independent array segments.
    /// </summary>
    internal sealed class MemoryPoolBlock : MemoryManager<byte>
    {
        private readonly int _offset;
        private readonly int _length;

        private int _pinCount;

        /// <summary>
        /// This object cannot be instantiated outside of the static Create method
        /// </summary>
        internal MemoryPoolBlock(SlabMemoryPool pool, MemoryPoolSlab slab, int offset, int length)
        {
            _offset = offset;
            _length = length;

            Pool = pool;
            Slab = slab;
        }

        /// <summary>
        /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
        /// </summary>
        public SlabMemoryPool Pool { get; }

        /// <summary>
        /// Back-reference to the slab from which this block was taken, or null if it is one-time-use memory.
        /// </summary>
        public MemoryPoolSlab Slab { get; }

        public override Memory<byte> Memory
        {
            get
            {
                if (!Slab.IsActive) ThrowHelper.ThrowObjectDisposedException(ExceptionArgument.MemoryPoolBlock);

                return CreateMemory(_length);
            }
        }


#if BLOCK_LEASE_TRACKING
        public bool IsLeased { get; set; }
        public string Leaser { get; set; }
#endif

        ~MemoryPoolBlock()
        {
            if (Slab != null && Slab.IsActive)
            {
               Debug.Assert(false, $"{Environment.NewLine}{Environment.NewLine}*** Block being garbage collected instead of returned to pool" +
#if BLOCK_LEASE_TRACKING
                   $": {Leaser}" +
#endif
                   $" ***{ Environment.NewLine}");

                // Need to make a new object because this one is being finalized
                Pool.Return(new MemoryPoolBlock(Pool, Slab, _offset, _length));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!Slab.IsActive) ThrowHelper.ThrowObjectDisposedException(ExceptionArgument.MemoryPoolBlock);

            if (Volatile.Read(ref _pinCount) > 0)
            {
                ThrowHelper.ThrowInvalidOperationException_ReturningPinnedBlock();
            }

            Pool.Return(this);
        }

        public override Span<byte> GetSpan() => new Span<byte>(Slab.Array, _offset, _length);

        public override MemoryHandle Pin(int byteOffset = 0)
        {
            if (!Slab.IsActive) ThrowHelper.ThrowObjectDisposedException(ExceptionArgument.MemoryPoolBlock);
            if (byteOffset < 0 || byteOffset > _length) ThrowHelper.ThrowArgumentOutOfRangeException(_length, byteOffset);

            Interlocked.Increment(ref _pinCount);
            unsafe
            {
                return new MemoryHandle((Slab.NativePointer + _offset + byteOffset).ToPointer(), default, this);
            }
        }

        protected override bool TryGetArray(out ArraySegment<byte> segment)
        {
            segment = new ArraySegment<byte>(Slab.Array, _offset, _length);
            return true;
        }

        public override void Unpin()
        {
            if (Interlocked.Decrement(ref _pinCount) < 0)
            {
                ThrowHelper.ThrowInvalidOperationException_ReferenceCountZero();
            }
        }

        public void Lease()
        {
#if BLOCK_LEASE_TRACKING
            Leaser = Environment.StackTrace;
            IsLeased = true;
#endif
        }
    }
}

#endif