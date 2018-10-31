// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Buffers.BuffersThrowHelper;

namespace System.Buffers
{
    public ref partial struct BufferReader<T> where T : unmanaged, IEquatable<T>
    {
        private SequencePosition _currentPosition;
        private SequencePosition _nextPosition;
        private bool _moreData;

        /// <summary>
        /// Create a <see cref="BufferReader{T}"/> over the given <see cref="ReadOnlySequence{T}"/>./>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferReader(ReadOnlySequence<T> buffer)
        {
            CurrentSpanIndex = 0;
            Consumed = 0;
            Sequence = buffer;
            _currentPosition = buffer.Start;

            GetFirstSpan(buffer, out ReadOnlySpan<T> first, out _nextPosition);
            CurrentSpan = first;
            _moreData = first.Length > 0;

            if (!buffer.IsSingleSegment && !_moreData)
            {
                _moreData = true;
                GetNextSpan();
            }
        }

        private const int FlagBitMask = 1 << 31;
        private const int IndexBitMask = ~FlagBitMask;

        // TODO:
        // Move to helper in ReadOnlySequence
        // https://github.com/dotnet/corefx/issues/33029
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetFirstSpan(in ReadOnlySequence<T> buffer, out ReadOnlySpan<T> first, out SequencePosition next)
        {
            first = default;
            next = default;
            SequencePosition start = buffer.Start;
            int startIndex = start.GetInteger();
            object startObject = start.GetObject();

            if (startObject != null)
            {
                SequencePosition end = buffer.End;
                int endIndex = end.GetInteger();
                bool isMultiSegment = startObject != end.GetObject();

                // A == 0 && B == 0 means SequenceType.MultiSegment
                if (startIndex >= 0)
                {
                    if (endIndex >= 0)  // SequenceType.MultiSegment
                    {
                        ReadOnlySequenceSegment<T> segment = (ReadOnlySequenceSegment<T>)startObject;
                        next = new SequencePosition(segment.Next, 0);
                        first = segment.Memory.Span;
                        if (isMultiSegment)
                        {
                            first = first.Slice(startIndex);
                        }
                        else
                        {
                            first = first.Slice(startIndex, endIndex - startIndex);
                        }
                    }
                    else
                    {
                        if (isMultiSegment)
                            ThrowInvalidOperationException_EndPositionNotReached();

                        first = new ReadOnlySpan<T>((T[])startObject, startIndex, (endIndex & IndexBitMask) - startIndex);
                    }
                }
                else
                {
                    first = GetFirstSpanSlow(startObject, startIndex, endIndex, isMultiSegment);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ReadOnlySpan<T> GetFirstSpanSlow(object startObject, int startIndex, int endIndex, bool isMultiSegment)
        {
            Debug.Assert(startIndex < 0 || endIndex < 0);
            if (isMultiSegment)
                ThrowInvalidOperationException_EndPositionNotReached();

            // The type == char check here is redundant. However, we still have it to allow
            // the JIT to see when that the code is unreachable and eliminate it.
            // A == 1 && B == 1 means SequenceType.String
            if (typeof(T) == typeof(char) && endIndex < 0)
            {
                var memory = (ReadOnlyMemory<T>)(object)((string)startObject).AsMemory();

                // No need to remove the FlagBitMask since (endIndex - startIndex) == (endIndex & ReadOnlySequence.IndexBitMask) - (startIndex & ReadOnlySequence.IndexBitMask)
                return memory.Span.Slice(startIndex & IndexBitMask, endIndex - startIndex);
            }
            else // endIndex >= 0, A == 1 && B == 0 means SequenceType.MemoryManager
            {
                startIndex &= IndexBitMask;
                return ((MemoryManager<T>)startObject).Memory.Span.Slice(startIndex, endIndex - startIndex);
            }
        }

        /// <summary>
        /// True when there is no more data in the <see cref="Sequence"/>.
        /// </summary>
        public bool End => !_moreData;

        /// <summary>
        /// The underlying <see cref="ReadOnlySequence{T}"/> for the reader.
        /// </summary>
        public ReadOnlySequence<T> Sequence { get; }

        /// <summary>
        /// The current position in the <see cref="Sequence"/>.
        /// </summary>
        public SequencePosition Position
            => Sequence.GetPosition(CurrentSpanIndex, _currentPosition);

        /// <summary>
        /// The current segment in the <see cref="Sequence"/>.
        /// </summary>
        public ReadOnlySpan<T> CurrentSpan { get; private set; }

        /// <summary>
        /// The index in the <see cref="CurrentSpan"/>.
        /// </summary>
        public int CurrentSpanIndex { get; private set; }

        /// <summary>
        /// The unread portion of the <see cref="CurrentSpan"/>.
        /// </summary>
        public ReadOnlySpan<T> UnreadSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CurrentSpan.Slice(CurrentSpanIndex);
        }

        /// <summary>
        /// The total number of {T}s processed by the reader.
        /// </summary>
        public long Consumed { get; private set; }

        /// <summary>
        /// Read the next value and advance the reader.
        /// </summary>
        /// <param name="value">The next value or default if at the end.</param>
        /// <returns>False if at the end of the reader.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out T value)
        {
            if (End)
            {
                value = default;
                return false;
            }

            value = CurrentSpan[CurrentSpanIndex];
            CurrentSpanIndex++;
            Consumed++;

            if (CurrentSpanIndex >= CurrentSpan.Length)
            {
                GetNextSpan();
            }

            return true;
        }

        /// <summary>
        /// Get the next segment with available space, if any.
        /// </summary>
        private void GetNextSpan()
        {
            SequencePosition previousNextPosition = _nextPosition;
            while (Sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> memory, advance: true))
            {
                _currentPosition = previousNextPosition;
                if (memory.Length > 0)
                {
                    CurrentSpan = memory.Span;
                    CurrentSpanIndex = 0;
                    return;
                }
                else
                {
                    CurrentSpan = default;
                    CurrentSpanIndex = 0;
                    previousNextPosition = _nextPosition;
                }
            }

            _moreData = false;
        }

        /// <summary>
        /// Move the reader ahead the specified number of positions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(long count)
        {
            const long TooBigOrNegative = unchecked((long)0xFFFFFFFF80000000);
            if ((count & TooBigOrNegative) == 0 && CurrentSpan.Length - CurrentSpanIndex > (int)count)
            {
                CurrentSpanIndex += (int)count;
                Consumed += count;
            }
            else
            {
                // Can't satisfy from the current span
                AdvanceToNextSpan(count);
            }
        }

        private void AdvanceToNextSpan(long count)
        {
            if (count < 0)
            {
                ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            Consumed += count;
            while (_moreData)
            {
                int remaining = CurrentSpan.Length - CurrentSpanIndex;

                if (remaining > count)
                {
                    CurrentSpanIndex += (int)count;
                    count = 0;
                    break;
                }

                CurrentSpanIndex += remaining;
                count -= remaining;
                Debug.Assert(count >= 0);

                GetNextSpan();

                if (count == 0)
                    break;
            }

            if (count != 0)
            {
                // Not enough space left- adjust for where we actually ended and throw
                Consumed -= count;
                ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }
        }
    }
}
