// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Represents a filter which allows only certain Unicode code points through.
    /// </summary>
    public sealed class CodePointFilter : ICodePointFilter
    {
        private AllowedCharsBitmap _allowedCharsBitmap;

        /// <summary>
        /// Instantiates the filter allowing only the 'Basic Latin' block of characters through.
        /// </summary>
        public CodePointFilter()
        {
            _allowedCharsBitmap = new AllowedCharsBitmap();
            AllowBlock(UnicodeBlocks.BasicLatin);
        }

        /// <summary>
        /// Instantiates the filter by cloning the allow list of another filter.
        /// </summary>
        public CodePointFilter([NotNull] ICodePointFilter other)
        {
            CodePointFilter otherAsCodePointFilter = other as CodePointFilter;
            if (otherAsCodePointFilter != null)
            {
                _allowedCharsBitmap = otherAsCodePointFilter.GetAllowedCharsBitmap();
            }
            else
            {
                _allowedCharsBitmap = new AllowedCharsBitmap();
                AllowFilter(other);
            }
        }

        /// <summary>
        /// Instantiates the filter where only the provided Unicode character blocks are
        /// allowed by the filter.
        /// </summary>
        /// <param name="allowedBlocks"></param>
        public CodePointFilter(params UnicodeBlock[] allowedBlocks)
        {
            _allowedCharsBitmap = new AllowedCharsBitmap();
            AllowBlocks(allowedBlocks);
        }

        /// <summary>
        /// Allows all characters in the specified Unicode character block through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowBlock([NotNull] UnicodeBlock block)
        {
            int firstCodePoint = block.FirstCodePoint;
            int blockSize = block.BlockSize;
            for (int i = 0; i < blockSize; i++)
            {
                _allowedCharsBitmap.AllowCharacter((char)(firstCodePoint + i));
            }
            return this;
        }

        /// <summary>
        /// Allows all characters in the specified Unicode character blocks through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowBlocks(params UnicodeBlock[] blocks)
        {
            if (blocks != null)
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    AllowBlock(blocks[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Allows the specified character through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowChar(char c)
        {
            _allowedCharsBitmap.AllowCharacter(c);
            return this;
        }

        /// <summary>
        /// Allows the specified characters through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowChars(params char[] chars)
        {
            if (chars != null)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    _allowedCharsBitmap.AllowCharacter(chars[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Allows all characters in the specified string through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowChars([NotNull] string chars)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                _allowedCharsBitmap.AllowCharacter(chars[i]);
            }
            return this;
        }

        /// <summary>
        /// Allows all characters approved by the specified filter through this filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowFilter([NotNull] ICodePointFilter filter)
        {
            foreach (var allowedCodePoint in filter.GetAllowedCodePoints())
            {
                // If the code point can't be represented as a BMP character, skip it.
                char codePointAsChar = (char)allowedCodePoint;
                if (allowedCodePoint == codePointAsChar)
                {
                    _allowedCharsBitmap.AllowCharacter(codePointAsChar);
                }
            }
            return this;
        }

        /// <summary>
        /// Disallows all characters in the specified Unicode character block through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidBlock([NotNull] UnicodeBlock block)
        {
            int firstCodePoint = block.FirstCodePoint;
            int blockSize = block.BlockSize;
            for (int i = 0; i < blockSize; i++)
            {
                _allowedCharsBitmap.ForbidCharacter((char)(firstCodePoint + i));
            }
            return this;
        }

        /// <summary>
        /// Disallows all characters in the specified Unicode character blocks through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidBlocks(params UnicodeBlock[] blocks)
        {
            if (blocks != null)
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    ForbidBlock(blocks[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Disallows the specified character through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidChar(char c)
        {
            _allowedCharsBitmap.ForbidCharacter(c);
            return this;
        }

        /// <summary>
        /// Disallows the specified characters through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidChars(params char[] chars)
        {
            if (chars != null)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    _allowedCharsBitmap.ForbidCharacter(chars[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Disallows all characters in the specified string through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidChars([NotNull] string chars)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                _allowedCharsBitmap.ForbidCharacter(chars[i]);
            }
            return this;
        }

        /// <summary>
        /// Retrieves the bitmap of allowed characters from this filter.
        /// The returned bitmap is a clone of the original bitmap to avoid unintentional modification.
        /// </summary>
        internal AllowedCharsBitmap GetAllowedCharsBitmap()
        {
            return _allowedCharsBitmap.Clone();
        }

        /// <summary>
        /// Gets an enumeration of all allowed code points.
        /// </summary>
        public IEnumerable<int> GetAllowedCodePoints()
        {
            for (int i = 0; i < 0x10000; i++)
            {
                if (_allowedCharsBitmap.IsCharacterAllowed((char)i))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Returns a value stating whether the given character is allowed through the filter.
        /// </summary>
        public bool IsCharacterAllowed(char c)
        {
            return _allowedCharsBitmap.IsCharacterAllowed(c);
        }
        
        /// <summary>
        /// Wraps the provided filter as a CodePointFilter, avoiding the clone if possible.
        /// </summary>
        internal static CodePointFilter Wrap(ICodePointFilter filter)
        {
            return (filter as CodePointFilter) ?? new CodePointFilter(filter);
        }
    }
}
