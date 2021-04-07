// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    /// <summary>
    /// Wraps an array allocated in the pinned object heap in a reusable block of managed memory
    /// </summary>
    internal sealed class MemoryPoolBlock : IMemoryOwner<byte>
    {
        internal MemoryPoolBlock(SlabMemoryPool pool, int length)
        {
            Pool = pool;

            var pinnedArray = GC.AllocateUninitializedArray<byte>(length, pinned: true);

            Memory = MemoryMarshal.CreateFromPinnedArray(pinnedArray, 0, pinnedArray.Length);
        }

        /// <summary>
        /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
        /// </summary>
        public SlabMemoryPool Pool { get; }

        public Memory<byte> Memory { get; }

        public void Dispose()
        {
            Pool.Return(this);
        }
    }
}
