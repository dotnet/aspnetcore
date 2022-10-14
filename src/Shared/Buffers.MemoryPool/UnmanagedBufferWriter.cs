// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> implementation for writing to unmanaged memory
/// that is owned by the writer.
/// </summary>
internal sealed unsafe class UnmanagedBufferWriter : IBufferWriter<byte>, IDisposable
{
    private readonly int _blockSize;
    private readonly List<IntPtr> _allocations = new();
    private byte* _currentBlock;
    private int _currentBlockCount;

    /// <summary>
    /// Instantiate an <see cref="UnmanagedBufferWriter"/> instance.
    /// </summary>
    /// <param name="blockSize">The unmanaged memory block size.</param>
    public UnmanagedBufferWriter(int blockSize)
    {
        _blockSize = blockSize;
        _currentBlockCount = -1;
    }

    /// <inheritdoc />
    public void Advance(int count)
    {
        Debug.Assert(count >= 0);

        _currentBlockCount -= count;
        if (_currentBlockCount > 0)
        {
            _currentBlock += count;
        }
        else
        {
            NewBlock();
        }
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0) => throw new NotImplementedException();

    /// <inheritdoc />
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Debug.Assert(sizeHint >= 0);

        // Allocation request that is beyond the block size
        if (sizeHint > _blockSize)
            return new Span<byte>(Alloc(sizeHint), sizeHint);

        // Check if there is enough room in the current block
        if (sizeHint > _currentBlockCount)
            NewBlock();

        return new Span<byte>(_currentBlock, _currentBlockCount);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var alloc in _allocations)
        {
            NativeLibrary.Free(alloc);
        }
    }

    /// <summary>
    /// Allocate a block of unmanaged memory that will is owned by this
    /// writer.
    /// </summary>
    /// <param name="size">The amount of memory to allocate in bytes.</param>
    /// <returns>The allocated memory.</returns>
    public byte* Alloc(int size)
    {
        var alloc = (byte*)NativeMemory.AlignedAlloc((nuint)size, 8);
        _allocations.Add((IntPtr)alloc);
        return alloc;
    }

    private void NewBlock()
    {
        Debug.Assert(_blockSize != 0);
        _currentBlock = Alloc(_blockSize);
        _currentBlockCount = _blockSize;
    }
}
