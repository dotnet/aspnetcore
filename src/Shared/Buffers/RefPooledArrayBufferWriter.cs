// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/corefx/blob/b0751dcd4a419ba6731dcaa7d240a8a1946c934c/src/System.Text.Json/src/System/Text/Json/Serialization/ArrayBufferWriter.cs

using System.Runtime.CompilerServices;

namespace System.Buffers;

/// <summary>
/// A high-performance struct-based IBufferWriter&lt;byte&gt; implementation that uses ArrayPool for allocations.
/// Designed for zero-allocation scenarios when used with generic methods via `allows ref struct` constraint.
/// </summary>
internal ref struct RefPooledArrayBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[] _buffer;
    private int _index;

    /// <summary>
    /// Initializes a new instance of StructArrayBufferWriter with a specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity to rent from the ArrayPool.</param>
    public RefPooledArrayBufferWriter(int initialCapacity)
    {
        if (initialCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity should be positive.");
        }

        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>
    /// Clears the buffer contents and returns the rented array to the ArrayPool.
    /// This must be called to properly clean up resources.
    /// </summary>
    public void Dispose()
    {
        if (_buffer == null)
        {
            return;
        }

        // Optionally clear the buffer before returning to pool (security)
        // Uncomment if needed for sensitive data:
        // _buffer.AsSpan(0, _index).Clear();

        ArrayPool<byte>.Shared.Return(_buffer, clearArray: false);
        _buffer = null!;
    }

    /// <summary>
    /// Gets the number of bytes written to the buffer.
    /// </summary>
    public int WrittenCount
    {
        get
        {
            ThrowIfDisposed();
            return _index;
        }
    }

    /// <summary>
    /// Gets the capacity of the underlying buffer.
    /// </summary>
    public int Capacity
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.Length;
        }
    }

    /// <summary>
    /// Gets the available space remaining in the buffer.
    /// </summary>
    public int FreeCapacity
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.Length - _index;
        }
    }

    /// <summary>
    /// Gets a memory segment representing the available space for writing.
    /// </summary>
    /// <param name="sizeHint">A hint about the minimum size needed. Ignored in this implementation as resizing is not performed.</param>
    /// <returns>A Memory&lt;byte&gt; segment for the available space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        ThrowIfDisposed();
        return _buffer.AsMemory(_index);
    }

    /// <summary>
    /// Gets a span representing the available space for writing.
    /// </summary>
    /// <param name="sizeHint">A hint about the minimum size needed. Ignored in this implementation as resizing is not performed.</param>
    /// <returns>A Span&lt;byte&gt; for the available space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        ThrowIfDisposed();
        return _buffer.AsSpan(_index);
    }

    /// <summary>
    /// Advances the write position by the specified count.
    /// </summary>
    /// <param name="count">The number of bytes written.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown if advancing would exceed buffer capacity.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        ThrowIfDisposed();

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        }

        if (_index > _buffer.Length - count)
        {
            throw new InvalidOperationException($"Cannot advance past the end of the buffer. Current position: {_index}, Capacity: {_buffer.Length}, Requested advance: {count}.");
        }

        _index += count;
    }

    /// <summary>
    /// Gets a span of the written data.
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.AsSpan(0, _index);
        }
    }

    /// <summary>
    /// Gets a memory segment of the written data.
    /// </summary>
    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.AsMemory(0, _index);
        }
    }

    /// <summary>
    /// Clears the buffer, resetting the write position to zero without deallocating.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        ThrowIfDisposed();
        _index = 0;
    }

    /// <summary>
    /// Resets the writer state, returning the buffer to the pool if it exists.
    /// Useful when reusing the struct in a loop.
    /// </summary>
    public void Reset()
    {
        Dispose();
        _index = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_buffer is null)
        {
            throw new ObjectDisposedException(nameof(RefPooledArrayBufferWriter), "The buffer writer has been disposed.");
        }
    }
}
