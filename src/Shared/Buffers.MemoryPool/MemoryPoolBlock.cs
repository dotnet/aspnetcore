// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Buffers;

/// <summary>
/// Wraps an array allocated in the pinned object heap in a reusable block of managed memory
/// </summary>
internal sealed class MemoryPoolBlock : IMemoryOwner<byte>
{
    internal MemoryPoolBlock(PinnedBlockMemoryPool pool, int length)
    {
        Pool = pool;

        var pinnedArray = GC.AllocateUninitializedArray<byte>(length, pinned: true);

        Memory = MemoryMarshal.CreateFromPinnedArray(pinnedArray, 0, pinnedArray.Length);
    }

    /// <summary>
    /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
    /// </summary>
    public PinnedBlockMemoryPool Pool { get; }

    public Memory<byte> Memory { get; }

    public void Dispose()
    {
        Pool.Return(this);
    }
}
