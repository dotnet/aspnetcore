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
    private T[]? _rentedBuffer;
    private Span<T> _buffer;
    private int _index;

    private const int MinimumBufferSize = 256;

    /// <summary>
    /// Initializes the <see cref="RefPooledArrayBufferWriter{T}"/> with initial buffer. 
    /// </summary>
    /// <param name="initialBuffer">The initial buffer to start writer with.</param>
    public RefPooledArrayBufferWriter(Span<T> initialBuffer)
    {
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
            ArrayPool<T>.Shared.Return(_rentedBuffer, clearArray: true);
            _buffer = null;
        }
    }

    /// <summary>
    /// Gets a span of the written data.
    /// </summary>
    public readonly ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            Debug.Assert(_index >= 0);
            return _buffer.Slice(0, _index);
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
#if NET
        throw new UnreachableException("RefPooledArrayBufferWriter does not support GetMemory");
#else
        throw new NotSupportedException("RefPooledArrayBufferWriter does not support GetMemory");
#endif
    }

    /// <summary>
    /// Gets a span representing the available space for writing.
    /// </summary>
    /// <param name="sizeHint">A hint about the minimum size needed. Ignored in this implementation as resizing is not performed.</param>
    /// <returns>A Span&lt;byte&gt; for the available space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int sizeHint = 0)
    {
        Debug.Assert(_index >= 0);
        CheckAndResizeBuffer(sizeHint);

        return _buffer.Slice(_index);
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
        Debug.Assert(_index >= 0);
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        }

        if (_index + count > _buffer.Length)
        {
            throw new InvalidOperationException($"Cannot advance past the end of the buffer. Current position: {_index}, Capacity: {_buffer.Length}, Requested advance: {count}.");
        }

        _index += count;
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint), actualValue: sizeHint, $"{nameof(sizeHint)} ('{sizeHint}') must be a non-negative value.");
        }
        if (sizeHint == 0)
        {
            sizeHint = MinimumBufferSize;
        }

        // initial buffer is still in use
        if (_rentedBuffer is null)
        {
            var bufferSpace = _buffer.Length - _index;
            if (bufferSpace < sizeHint)
            {
                // initial buffer is not enough, we need to start renting from the pool
                var rentedInitialSize = _buffer.Length + Math.Max(sizeHint, _buffer.Length);
                _rentedBuffer = ArrayPool<T>.Shared.Rent(rentedInitialSize);

                _buffer.CopyTo(_rentedBuffer);
                _buffer = _rentedBuffer;

                Debug.Assert(_rentedBuffer.Length - _index > 0);
                Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
            }

            return;
        }

        var availableSpace = _buffer.Length - _index;
        if (sizeHint <= availableSpace)
        {
            return;
        }

        // we are using rented buffer, so grow it if needed
        var growBy = Math.Max(sizeHint, _buffer.Length);
        var newSize = checked(_buffer.Length + growBy);

        var oldBuffer = _rentedBuffer;
        _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

        Debug.Assert(oldBuffer.Length >= _index);
        Debug.Assert(_rentedBuffer.Length >= _index);

        var previousBuffer = oldBuffer.AsSpan(0, _index);
        previousBuffer.CopyTo(_rentedBuffer);
        ArrayPool<T>.Shared.Return(oldBuffer);

        _buffer = _rentedBuffer;

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }
}
