// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Framework.WebEncoders
{
    internal struct AllowedCharsBitmap
    {
        private const int ALLOWED_CHARS_BITMAP_LENGTH = 0x10000 / (8 * sizeof(uint));
        private uint[] _allowedCharsBitmap;

        public AllowedCharsBitmap()
        {
            _allowedCharsBitmap = new uint[ALLOWED_CHARS_BITMAP_LENGTH];
        }

        // Marks a character as allowed (can be returned unencoded)
        public void AllowCharacter(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            _allowedCharsBitmap[index] |= 0x1U << offset;
        }

        public AllowedCharsBitmap Clone()
        {
            AllowedCharsBitmap retVal;
            retVal._allowedCharsBitmap = (uint[])this._allowedCharsBitmap.Clone();
            return retVal;
        }

        // Marks a character as forbidden (must be returned encoded)
        public void ForbidCharacter(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            _allowedCharsBitmap[index] &= ~(0x1U << offset);
        }
        
        public void ForbidUndefinedCharacters()
        {
            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            uint[] definedCharactersBitmap = UnicodeHelpers.GetDefinedCharacterBitmap();
            Debug.Assert(definedCharactersBitmap.Length == _allowedCharsBitmap.Length);
            for (int i = 0; i < _allowedCharsBitmap.Length; i++)
            {
                _allowedCharsBitmap[i] &= definedCharactersBitmap[i];
            }
        }

        // Determines whether the given character can be returned unencoded.
        public bool IsCharacterAllowed(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            return ((_allowedCharsBitmap[index] >> offset) & 0x1U) != 0;
        }
    }
}
