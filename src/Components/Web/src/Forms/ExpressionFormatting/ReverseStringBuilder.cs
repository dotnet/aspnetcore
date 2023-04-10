// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Microsoft.AspNetCore.Components.Forms;

internal unsafe ref struct ReverseStringBuilder
{
    public const int FixedMemorySlotLength = 4;
    public const int MinimumRentedMemorySize = 128;

    private static readonly IntKeyedMemoryPool<char> s_memoryPool = IntKeyedMemoryPool<char>.Shared;

    private int _nextEndIndex;
    private int _filledMemoryTotalLength = 0;
    private int _rentedMemoryIndex;
    private Span<char> _currentBuffer;
    private List<int>? _fallbackRentedMemoryIds;

    private fixed int _rentedMemoryIds[FixedMemorySlotLength];

    // For testing.
    internal readonly int RentedMemoryIndex => _rentedMemoryIndex;

    public ReverseStringBuilder(int conservativeEstimatedStringLength)
    {
        _rentedMemoryIds[_rentedMemoryIndex] = s_memoryPool.Rent(conservativeEstimatedStringLength, out var initialRentedMemory);
        _currentBuffer = initialRentedMemory.Span;
        _nextEndIndex = _currentBuffer.Length;
        _rentedMemoryIndex = 0;
    }

    public ReverseStringBuilder(Span<char> initialBuffer)
    {
        _rentedMemoryIndex = -1;
        _currentBuffer = initialBuffer;
        _nextEndIndex = _currentBuffer.Length;
    }

    public void InsertFront(scoped ReadOnlySpan<char> span)
    {
        var startIndex = _nextEndIndex - span.Length;
        if (startIndex >= 0)
        {
            // The common case. There is enough space in the current buffer to copy the given span.
            // No additional work needs to be done here after the copy.
            span.CopyTo(_currentBuffer[startIndex..]);
            _nextEndIndex = startIndex;
            return;
        }

        // There wasn't enough space in the current buffer.
        // What we do next depends on whether we're writing to the provided "initial" buffer or a rented one.

        if (_rentedMemoryIndex < 0)
        {
            // We've been writing to a stack-allocated buffer, but there is no more room on the stack.
            // We rent new memory with a length sufficiently larger than the initial buffer
            // and copy the contents over.

            // Using multiple buffers requires us to use string.Create() to generate the final string.
            // We can't use the stack-allocated buffer in that case, because it would
            // require moving the Span<char> (which by definition lives on the stack) to the heap.
            // Hence the reason we do this copy up front. Making the new buffer sufficiently large
            // also gives us some wiggle room to take the "happy path" during string generation and
            // avoid using string.Create().

            // If ever, this should ideally only happen in one case: We're seeing an expression for
            // the first time and its length was larger than the stack-allocated buffer.
            // Assuming we have an accurate but conservative length estimate, we will either never
            // need to rent from the array pool (if the string is short enough), or we'll ask for an
            // appropriate intial array size so we never run out of space.

            var remainingLength = -startIndex;
            var sizeToRent = _currentBuffer.Length + Math.Max(MinimumRentedMemorySize, remainingLength * 2);
            var rentedMemoryId = s_memoryPool.Rent(sizeToRent, out var rentedMemory);

            _rentedMemoryIndex = 0;
            _rentedMemoryIds[_rentedMemoryIndex] = rentedMemoryId;

            var newBuffer = rentedMemory.Span;
            _nextEndIndex = newBuffer.Length - _currentBuffer.Length;
            _currentBuffer.CopyTo(newBuffer[_nextEndIndex..]);
            _currentBuffer = newBuffer;

            startIndex = _nextEndIndex - span.Length;
            span.CopyTo(_currentBuffer[startIndex..]);
            _nextEndIndex = startIndex;
        }
        else
        {
            // We can't fit the whole string in the current heap-allocated buffer.
            // Copy as much as we can to the current buffer, rent a new buffer, and
            // continue copying the remaining contents.
            var remainingLength = -startIndex;
            span[remainingLength..].CopyTo(_currentBuffer);
            span = span[..remainingLength];

            var sizeToRent = Math.Max(MinimumRentedMemorySize, remainingLength * 2);
            var rentedMemoryId = s_memoryPool.Rent(sizeToRent, out var rentedMemory);

            _rentedMemoryIndex++;
            _filledMemoryTotalLength += _currentBuffer.Length;

            if (_rentedMemoryIndex < FixedMemorySlotLength)
            {
                _rentedMemoryIds[_rentedMemoryIndex] = rentedMemoryId;
            }
            else
            {
                _fallbackRentedMemoryIds ??= new();
                _fallbackRentedMemoryIds.Add(rentedMemoryId);
            }

            _currentBuffer = rentedMemory.Span;

            startIndex = _currentBuffer.Length - remainingLength;
            span.CopyTo(_currentBuffer[startIndex..]);
            _nextEndIndex = startIndex;
        }
    }

    public void InsertFront<TNumber>(TNumber value) where TNumber : INumber<TNumber>, ISpanFormattable
    {
        // This is large enough for any integer value (10 digits plus the possible sign).
        // We won't try to optimize for anything larger.
        Span<char> result = stackalloc char[11];

        if (value.TryFormat(result, out var charsWritten, default, default))
        {
            InsertFront(result[..charsWritten]);
        }
        else
        {
            InsertFront(value.ToString(null, CultureInfo.InvariantCulture));
        }
    }

    public override string ToString()
    {
        if (_rentedMemoryIndex <= 0)
        {
            // Only one buffer was used, so we can create the string directly.
            // This will happen in the most common cases:
            // 1. We didn't have an initial string length estimate, but the string was shorter than the size
            //    of the initial buffer (either stack-allocated or copied to the heap). Or...
            // 2. We were provided a string length estimate, and the resulting string was shorter than that estimate.
            return new(_currentBuffer[_nextEndIndex..]);
        }

        var totalLength = _filledMemoryTotalLength + (_currentBuffer.Length - _nextEndIndex);
        return string.Create(totalLength, new BufferCollection(ref this), CombineBuffers);
    }

    public readonly void Dispose()
    {
        for (var i = 0; i <= _rentedMemoryIndex; i++)
        {
            // TODO: We can probably avoid doing this check on every iteration.
            // Note that this also isn't super important to optimize because it will
            // be extremely rare to ever rent more than 2 buffers.
            var memoryId = i < FixedMemorySlotLength
                ? _rentedMemoryIds[i]
                : _fallbackRentedMemoryIds?[i - FixedMemorySlotLength] ?? throw new UnreachableException();
            s_memoryPool.Return(memoryId);
        }
    }

    private static void CombineBuffers(Span<char> span, BufferCollection buffers)
    {
        Debug.Assert(buffers.Length > 0);

        for (var i = buffers.Length - 1; i >= 0; i--)
        {
            Debug.Assert(span.Length > 0);

            var rentedBuffer = buffers[i];
            rentedBuffer.CopyTo(span);
            span = span[rentedBuffer.Length..];
        }

        Debug.Assert(span.Length == 0);
    }

    private unsafe struct BufferCollection
    {
        private readonly int _lastBufferEndIndex;
        private readonly List<int>? _fallbackRentedMemoryIds;

        private fixed int _rentedMemoryIds[FixedMemorySlotLength];

        public readonly int Length { get; }

        public BufferCollection(ref ReverseStringBuilder source)
        {
            _lastBufferEndIndex = source._nextEndIndex;
            _fallbackRentedMemoryIds = source._fallbackRentedMemoryIds;

            for (var i = 0; i < FixedMemorySlotLength; i++)
            {
                _rentedMemoryIds[i] = source._rentedMemoryIds[i];
            }

            Length = source._rentedMemoryIndex + 1;
        }

        public readonly Span<char> this[int index]
        {
            get
            {
                // TODO: We can probably move some of this logic to the caller
                // and avoid doing this check on every iteration of the loop.
                // Note that this also isn't super important to optimize because it will
                // be extremely rare to ever rent more than 2 buffers.
                var memoryId = index < FixedMemorySlotLength
                    ? _rentedMemoryIds[index]
                    : _fallbackRentedMemoryIds?[index - FixedMemorySlotLength] ?? throw new ArgumentOutOfRangeException(nameof(index));

                var rentedMemory = s_memoryPool.GetRentedMemory(memoryId);
                var rentedSpan = rentedMemory.Span;

                if (index == Length - 1)
                {
                    rentedSpan = rentedSpan[_lastBufferEndIndex..];
                }

                return rentedSpan;
            }
        }
    }
}
