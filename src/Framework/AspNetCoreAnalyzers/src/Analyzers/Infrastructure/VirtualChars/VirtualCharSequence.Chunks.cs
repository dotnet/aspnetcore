// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

internal partial struct VirtualCharSequence
{
    /// <summary>
    /// Abstraction over a contiguous chunk of <see cref="VirtualChar"/>s.  This
    /// is used so we can expose <see cref="VirtualChar"/>s over an <see cref="ImmutableArray{VirtualChar}"/>
    /// or over a <see cref="string"/>.  The latter is especially useful for reducing
    /// memory usage in common cases of string tokens without escapes.
    /// </summary>
    private abstract partial class Chunk
    {
        protected Chunk()
        {
        }

        public abstract int Length { get; }
        public abstract VirtualChar this[int index] { get; }
        public abstract VirtualChar? Find(int position);
    }

    /// <summary>
    /// Thin wrapper over an actual <see cref="ImmutableSegmentedList{T}"/>.
    /// This will be the common construct we generate when getting the
    /// <see cref="Chunk"/> for a string token that has escapes in it.
    /// </summary>
    private class ImmutableSegmentedListChunk : Chunk
    {
        private readonly ImmutableList<VirtualChar> _array;

        public ImmutableSegmentedListChunk(ImmutableList<VirtualChar> array)
            => _array = array;

        public override int Length => _array.Count;
        public override VirtualChar this[int index] => _array[index];

        public override VirtualChar? Find(int position)
        {
            if (_array.IsEmpty)
            {
                return null;
            }
            if (position < _array[0].Span.Start || position >= _array[_array.Count - 1].Span.End)
            {
                return null;
            }
            var index = BinarySearch(_array, position, static (ch, position) =>
            {
                if (position < ch.Span.Start)
                {
                    return 1;
                }

                if (position >= ch.Span.End)
                {
                    return -1;
                }

                return 0;
            });
            Debug.Assert(index >= 0);
            return _array[index];
        }
    }

    internal static int BinarySearch<TElement, TValue>(ImmutableList<TElement> array, TValue value, Func<TElement, TValue, int> comparer)
    {
        int low = 0;
        int high = array.Count - 1;

        while (low <= high)
        {
            int middle = low + ((high - low) >> 1);
            int comparison = comparer(array[middle], value);

            if (comparison == 0)
            {
                return middle;
            }

            if (comparison > 0)
            {
                high = middle - 1;
            }
            else
            {
                low = middle + 1;
            }
        }

        return ~low;
    }

    /// <summary>
    /// Represents a <see cref="Chunk"/> on top of a normal
    /// string.  This is the common case of the type of the sequence we would
    /// create for a normal string token without any escapes in it.
    /// </summary>
    private class StringChunk : Chunk
    {
        private readonly int _firstVirtualCharPosition;

        /// <summary>
        /// The underlying string that we're returning virtual chars from.  Note:
        /// this will commonly include things like quote characters.  Clients who
        /// do not want that should then ask for an appropriate <see cref="VirtualCharSequence.GetSubSequence"/>
        /// back that does not include those characters.
        /// </summary>
        private readonly string _underlyingData;

        public StringChunk(int firstVirtualCharPosition, string data)
        {
            _firstVirtualCharPosition = firstVirtualCharPosition;
            _underlyingData = data;
        }

        public override int Length => _underlyingData.Length;

        public override VirtualChar? Find(int position)
        {
            var stringIndex = position - _firstVirtualCharPosition;
            if (stringIndex < 0 || stringIndex >= _underlyingData.Length)
            {
                return null;
            }

            return this[stringIndex];
        }

        public override VirtualChar this[int index]
        {
            get
            {
#if DEBUG
                // We should never have a properly paired high/low surrogate in a StringChunk. We are only created
                // when the string has the same number of chars as there are VirtualChars.
                if (char.IsHighSurrogate(_underlyingData[index]))
                {
                    Debug.Assert(index + 1 >= _underlyingData.Length ||
                                 !char.IsLowSurrogate(_underlyingData[index + 1]));
                }
#endif

                var span = new TextSpan(_firstVirtualCharPosition + index, length: 1);
                var ch = _underlyingData[index];
                return char.IsSurrogate(ch)
                    ? VirtualChar.Create(ch, span)
                    : VirtualChar.Create(new Rune(ch), span);
            }
        }
    }
}
