// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Represents a contiguous range of Unicode code points.
    /// </summary>
    /// <remarks>
    /// Currently only the Basic Multilingual Plane is supported.
    /// </remarks>
    public sealed class UnicodeRange
    {
        /// <summary>
        /// Creates a new <see cref="UnicodeRange"/>.
        /// </summary>
        /// <param name="firstCodePoint">The first code point in the range.</param>
        /// <param name="rangeSize">The number of code points in the range.</param>
        public UnicodeRange(int firstCodePoint, int rangeSize)
        {
            // Parameter checking: the first code point and last code point must
            // lie within the BMP. See http://unicode.org/faq/blocks_ranges.html for more info.
            if (firstCodePoint < 0 || firstCodePoint > 0xFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(firstCodePoint));
            }
            if (rangeSize < 0 || ((long)firstCodePoint + (long)rangeSize > 0x10000))
            {
                throw new ArgumentOutOfRangeException(nameof(rangeSize));
            }

            FirstCodePoint = firstCodePoint;
            RangeSize = rangeSize;
        }

        /// <summary>
        /// The first code point in this range.
        /// </summary>
        public int FirstCodePoint { get; }

        /// <summary>
        /// The number of code points in this range.
        /// </summary>
        public int RangeSize { get; }

        /// <summary>
        /// Creates a new <see cref="UnicodeRange"/> from a span of characters.
        /// </summary>
        /// <param name="firstChar">The first character in the range.</param>
        /// <param name="lastChar">The last character in the range.</param>
        /// <returns>The <see cref="UnicodeRange"/> representing this span.</returns>
        public static UnicodeRange FromSpan(char firstChar, char lastChar)
        {
            if (lastChar < firstChar)
            {
                throw new ArgumentOutOfRangeException(nameof(lastChar));
            }

            return new UnicodeRange(firstChar, 1 + (int)(lastChar - firstChar));
        }
    }
}
