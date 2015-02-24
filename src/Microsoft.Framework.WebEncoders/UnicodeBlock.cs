// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Represents a range of Unicode code points.
    /// </summary>
    /// <remarks>
    /// Currently only the Basic Multilingual Plane is supported.
    /// </remarks>
    public sealed class UnicodeBlock
    {
        /// <summary>
        /// Creates a new representation of a Unicode block given the first code point
        /// in the block and the number of code points in the block.
        /// </summary>
        public UnicodeBlock(int firstCodePoint, int blockSize)
        {
            // Parameter checking: the first code point must be U+nnn0, the block size must
            // be a multiple of 16 bytes, and we can't span planes.
            // See http://unicode.org/faq/blocks_ranges.html for more info.
            if (firstCodePoint < 0 || firstCodePoint > 0xFFFF || ((firstCodePoint & 0xF) != 0))
            {
                throw new ArgumentOutOfRangeException(nameof(firstCodePoint));
            }
            if (blockSize < 0 || (blockSize % 16 != 0) || ((long)firstCodePoint + (long)blockSize > 0x10000))
            {
                throw new ArgumentOutOfRangeException(nameof(blockSize));
            }

            FirstCodePoint = firstCodePoint;
            BlockSize = blockSize;
        }

        /// <summary>
        /// The number of code points in this block.
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// The first code point in this block.
        /// </summary>
        public int FirstCodePoint { get; }

        public static UnicodeBlock FromCharacterRange(char firstChar, char lastChar)
        {
            // Parameter checking: the first code point must be U+nnn0 and the last
            // code point must be U+nnnF. We already can't span planes since 'char'
            // allows only Basic Multilingual Plane characters.
            // See http://unicode.org/faq/blocks_ranges.html for more info.
            if ((firstChar & 0xF) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstChar));
            }
            if (lastChar < firstChar || (lastChar & 0xF) != 0xF)
            {
                throw new ArgumentOutOfRangeException(nameof(lastChar));
            }

            return new UnicodeBlock(firstChar, 1 + (int)(lastChar - firstChar));
        }
    }
}
