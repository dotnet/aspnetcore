// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/corefx/blob/b0751dcd4a419ba6731dcaa7d240a8a1946c934c/src/System.Text.Json/src/System/Text/Json/Serialization/ArrayBufferWriter.cs

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers;

/// <summary>
/// A high-performance struct-based IBufferWriter&lt;byte&gt; implementation that uses ArrayPool for allocations.
/// Designed for zero-allocation scenarios when used with generic methods via `allows ref struct` constraint.
/// </summary>
internal ref struct RefPooledArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[] _rentedBuffer;
    private Span<T> _buffer;
    private int _index;

    private const int MinimumBufferSize = 256;

    /// <summary>
    /// Initializes the <see cref="RefPooledArrayBufferWriter{T}"/> with initial buffer. 
    /// </summary>
    /// <param name="initialBuffer">The initial buffer to start writer with.</param>
    public RefPooledArrayBufferWriter(Span<T> initialBuffer)
    {
        // no rented buffer initially - only if we need to grow over the limits of initialBuffer
        _rentedBuffer = null!;

        _buffer = initialBuffer;
        _index = 0;
    }

    /// <summary>
    /// Clears the buffer contents and returns the rented array to the ArrayPool.
    /// This must be called to properly clean up resources.
    /// </summary>
    public void Dispose()
    {
        // to avoid `bool isDisposed` field, we can use negative index as disposed marker
        _index = -1;

        if (_rentedBuffer is not null)
        {
            // Optionally clear the buffer before returning to pool (security)
            // Uncomment if needed for sensitive data:
            // _buffer.AsSpan(0, _index).Clear();

            ArrayPool<T>.Shared.Return(_rentedBuffer, clearArray: false);
            _buffer = null!;
        }
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
            return _rentedBuffer is not null ? _rentedBuffer.Length : _buffer.Length;
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
            var length = _rentedBuffer is not null ? _rentedBuffer.Length : _buffer.Length;
            return length - _index;
        }
    }

    /// <summary>
    /// Gets a span of the written data.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            ThrowIfDisposed();
            return _rentedBuffer is not null ? _rentedBuffer.AsSpan(0, _index) : _buffer.Slice(0, _index);
        }
    }

    /// <summary>
    /// Gets a memory segment of the written data.
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            ThrowIfDisposed();
            if (_rentedBuffer is not null)
            {
                return _rentedBuffer.AsMemory(0, _index);
            }

            // either we throw, or copy the Span<T> data into a heap-allocated array (via pooling), and then return Memory<T>.
            // For demonstration, that it should not be used with Memory<T> unless done something specific, we throw here.
            throw new InvalidOperationException("Cannot convert Span<T> to Memory<T>");
        }
    }

    /// <summary>
    /// Gets a memory segment representing the available space for writing.
    /// </summary>
    /// <param name="sizeHint">A hint about the minimum size needed. Ignored in this implementation as resizing is not performed.</param>
    /// <returns>A Memory&lt;byte&gt; segment for the available space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        ThrowIfDisposed();
        CheckAndResizeBuffer(sizeHint);
        if (_rentedBuffer is not null)
        {
            return _rentedBuffer.AsMemory(_index);
        }

        // either we throw, or copy the Span<T> data into a heap-allocated array (via pooling), and then return Memory<T>.
        // For demonstration, that it should not be used with Memory<T> unless done something specific, we throw here.
        throw new InvalidOperationException("Cannot convert Span<T> to Memory<T>");
    }

    /// <summary>
    /// Gets a span representing the available space for writing.
    /// </summary>
    /// <param name="sizeHint">A hint about the minimum size needed. Ignored in this implementation as resizing is not performed.</param>
    /// <returns>A Span&lt;byte&gt; for the available space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int sizeHint = 0)
    {
        ThrowIfDisposed();
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer is not null
            ? _rentedBuffer.AsSpan(_index)
            : _buffer.Slice(_index);
    }

    public void Advance(uint count)
        => Advance((int)count);

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

        var canAdvance = _rentedBuffer is not null
            ? _index + count < _rentedBuffer.Length
            : _index + count < _buffer.Length;

        if (!canAdvance)
        {
            throw new InvalidOperationException($"Cannot advance past the end of the buffer. Current position: {_index}, Capacity: {_buffer.Length}, Requested advance: {count}.");
        }
        _index += count;
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (sizeHint <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint), actualValue: sizeHint, $"{nameof(sizeHint)} ('{sizeHint}') must be a non-negative value.");
        }
        if (sizeHint == 0)
        {
            sizeHint = MinimumBufferSize;
        }

        if (_rentedBuffer is null)
        {
            var bufferSpace = _buffer.Length - _index;
            if (bufferSpace < sizeHint)
            {
                // initial buffer is not enough, we need to start renting from the pool
                var rentedInitialSize = _buffer.Length + Math.Max(sizeHint, _buffer.Length);
                _rentedBuffer = ArrayPool<T>.Shared.Rent(rentedInitialSize);

                _buffer.CopyTo(_rentedBuffer);
                _buffer.Clear();

                Debug.Assert(_rentedBuffer.Length - _index > 0);
                Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
            }

            return;
        }

        // we are already using rented buffer, so grow it if needed
        var availableSpace = _rentedBuffer.Length - _index;
        if (sizeHint > availableSpace)
        {
            var growBy = Math.Max(sizeHint, _rentedBuffer.Length);
            var newSize = checked(_rentedBuffer.Length + growBy);

            var oldBuffer = _rentedBuffer;
            _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            var previousBuffer = oldBuffer.AsSpan(0, _index);
            previousBuffer.CopyTo(_rentedBuffer);
            previousBuffer.Clear();
            ArrayPool<T>.Shared.Return(oldBuffer);
        }
        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }

    /// <summary>
    /// Clears the buffer, resetting the write position to zero without deallocating.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        ThrowIfDisposed();
        if (_rentedBuffer is not null)
        {
            _rentedBuffer?.AsSpan(0, _index).Clear();
        }
        else
        {
            _buffer.Slice(0, _index).Clear();
        }
        _index = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_index < 0)
        {
            throw new ObjectDisposedException(nameof(IBufferWriter<>), "The buffer writer has been disposed.");
        }
    }
}
