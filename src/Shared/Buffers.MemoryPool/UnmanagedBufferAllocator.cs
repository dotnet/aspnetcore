// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Allocator that manages blocks of unmanaged memory.
/// </summary>
internal unsafe struct UnmanagedBufferAllocator : IDisposable
{
    private readonly int _blockSize;
    private int _currentBlockCount;
    private void** _currentAlloc;
    private byte* _currentBlock;

    /// <summary>
    /// The default block size for the allocator.
    /// </summary>
    /// <remarks>
    /// This size assumes a common page size and provides an accommodation
    /// for the pointer chain used to track allocated blocks.
    /// </remarks>
    public static int DefaultBlockSize => 4096 - sizeof(void*);

    /// <summary>
    /// Instantiate an <see cref="UnmanagedBufferAllocator"/> instance.
    /// </summary>
    /// <param name="blockSize">The unmanaged memory block size in bytes.</param>
    public UnmanagedBufferAllocator(int blockSize = 0)
    {
        Debug.Assert(_blockSize >= 0);
        _blockSize = blockSize == 0 ? DefaultBlockSize : blockSize;
        _currentBlockCount = -1;
        _currentAlloc = null;
        _currentBlock = null;
    }

    /// <summary>
    /// Allocate the requested amount of space from the allocator.
    /// </summary>
    /// <typeparam name="T">The type requested</typeparam>
    /// <param name="count">The count in <typeparamref name="T"/> units</param>
    /// <returns>A pointer to the reserved memory.</returns>
    /// <remarks>
    /// The allocated memory is uninitialized.
    /// </remarks>
    public T* AllocAsPointer<T>(int count) where T : unmanaged
    {
        int toAlloc = checked(count * sizeof(T));
        Span<byte> alloc = GetSpan(toAlloc, out bool mustCommit);
        if (mustCommit)
        {
            Commit(toAlloc);
        }

        return (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(alloc));
    }

    /// <summary>
    /// Allocate the requested amount of space from the allocator.
    /// </summary>
    /// <typeparam name="T">The type requested</typeparam>
    /// <param name="count">The count in <typeparamref name="T"/> units</param>
    /// <returns>A Span to the reserved memory.</returns>
    /// <remarks>
    /// The allocated memory is uninitialized.
    /// </remarks>
    public Span<T> AllocAsSpan<T>(int count) where T : unmanaged
    {
        return new Span<T>(AllocAsPointer<T>(count), count);
    }

    /// <summary>
    /// Get pointer to bytes for the supplied string in UTF-8.
    /// </summary>
    /// <param name="myString">The string</param>
    /// <param name="length">The length of the returned byte buffer.</param>
    /// <returns>A pointer to the buffer of bytes</returns>
    public byte* GetHeaderEncodedBytes(string myString, out int length)
    {
        Debug.Assert(myString is not null);

        // Compute the maximum amount of bytes needed for the given string.
        // Include an extra byte for the null terminator.
        int maxAlloc = checked(Encoding.UTF8.GetMaxByteCount(myString.Length) + 1);
        Span<byte> buffer = GetSpan(maxAlloc, out bool mustCommit);
        length = Encoding.UTF8.GetBytes(myString, buffer);

        // Write a null terminator - the GetBytes() API doesn't add one.
        buffer[length] = 0;

        if (mustCommit)
        {
            // Let the writer know how much was used. Plus 1 for the null terminator above.
            Commit(length + 1);
        }

        return (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        void** curr = _currentAlloc;
        while (curr != null)
        {
            // Follow the pointer chain to delete all allocations.
            void** next = (void**)*curr;
            NativeMemory.Free(curr);
            curr = next;
        }
    }

    private Span<byte> GetSpan(int sizeHint, out bool mustCommit)
    {
        Debug.Assert(sizeHint >= 0);

        // Allocation request that is beyond the block size
        if (sizeHint > _blockSize)
        {
            // When the block size is exceeded, the caller
            // can ignore committing because the shared block isn't
            // being used. Instead we are allocating an exclusive block.
            mustCommit = false;
            return new Span<byte>(Alloc(sizeHint), sizeHint);
        }

        // Check if there is enough room in the current block
        if (sizeHint > _currentBlockCount)
        {
            NewBlock();
        }

        mustCommit = true;
        return new Span<byte>(_currentBlock, _currentBlockCount);
    }

    private void Commit(int count)
    {
        Debug.Assert(count >= 0);

        // We always consume pointer alignment sizes to ensure
        // the next space to return is always at least pointer aligned.
        count = (count + sizeof(void*) - 1) & ~(sizeof(void*) - 1);

        _currentBlockCount -= count;
        if (_currentBlockCount > 0)
        {
            _currentBlock += count;
        }
    }

    private byte* Alloc(int size)
    {
        // Allocate an extra pointer to create the allocation chain
        var newBlock = (void**)NativeMemory.Alloc((nuint)(size + sizeof(void*)));

        // Use the first pointer in the allocation to store the
        // previous block address.
        *newBlock = _currentAlloc;
        _currentAlloc = newBlock;

        return (byte*)&newBlock[1];
    }

    private void NewBlock()
    {
        _currentBlock = Alloc(_blockSize);
        _currentBlockCount = _blockSize;
    }
}
