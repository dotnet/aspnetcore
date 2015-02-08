// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Microsoft.AspNet.WebUtilities.Encoders
{
    /// <summary>
    /// A class which can perform HTML encoding given an allow list of characters which
    /// can be represented unencoded.
    /// </summary>
    /// <remarks>
    /// Once constructed, instances of this class are thread-safe for multiple callers.
    /// </remarks>
    public unsafe sealed class HtmlEncoder : IHtmlEncoder
    {
        // The default HtmlEncoder (Basic Latin), instantiated on demand
        private static HtmlEncoder _defaultEncoder;

        // A bitmap of characters which are allowed to be returned unescaped.
        private readonly uint[] _allowedCharsBitmap = new uint[0x10000 / 32];

        /// <summary>
        /// Instantiates an encoder using the 'Basic Latin' code table as the allow list.
        /// </summary>
        public HtmlEncoder()
            : this(CodePointFilters.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom allow list of characters.
        /// </summary>
        public HtmlEncoder(params ICodePointFilter[] filters)
        {
            if (filters == null)
            {
                return; // no characters are allowed, just no-op immediately
            }

            // Punch a hole for each allowed code point across all filters (this is an OR).
            // We don't allow supplementary (astral) characters for now.
            foreach (var filter in filters)
            {
                foreach (var codePoint in filter.GetAllowedCodePoints())
                {
                    if (!UnicodeHelpers.IsSupplementaryCodePoint(codePoint))
                    {
                        AllowCharacter((char)codePoint);
                    }
                }
            }

            // Forbid characters that are special in HTML
            ForbidCharacter('<');
            ForbidCharacter('>');
            ForbidCharacter('&');
            ForbidCharacter('\''); // can be used to escape attributes
            ForbidCharacter('\"'); // can be used to escape attributes
            ForbidCharacter('+'); // technically not HTML-specific, but can be used to perform UTF7-based attacks

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zl, Zp)
            uint[] definedCharactersBitmap = UnicodeHelpers.GetDefinedCharacterBitmap();
            Debug.Assert(definedCharactersBitmap.Length == _allowedCharsBitmap.Length);
            for (int i = 0; i < _allowedCharsBitmap.Length; i++)
            {
                _allowedCharsBitmap[i] &= definedCharactersBitmap[i];
            }
        }

        /// <summary>
        /// A default instance of the HtmlEncoder, equivalent to allowing only
        /// the 'Basic Latin' character range.
        /// </summary>
        public static HtmlEncoder Default
        {
            get
            {
                HtmlEncoder defaultEncoder = Volatile.Read(ref _defaultEncoder);
                if (defaultEncoder == null)
                {
                    defaultEncoder = new HtmlEncoder();
                    Volatile.Write(ref _defaultEncoder, defaultEncoder);
                }
                return defaultEncoder;
            }
        }

        // Marks a character as allowed (can be returned unencoded)
        private void AllowCharacter(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            _allowedCharsBitmap[index] |= 0x1U << offset;
        }

        // Marks a character as forbidden (must be returned encoded)
        private void ForbidCharacter(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            _allowedCharsBitmap[index] &= ~(0x1U << offset);
        }

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public string HtmlEncode(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }

            // Quick check: does the string need to be encoded at all?
            // If not, just return the input string as-is.
            for (int i = 0; i < value.Length; i++)
            {
                if (!IsCharacterAllowed(value[i]))
                {
                    return HtmlEncodeImpl(value, i);
                }
            }
            return value;
        }

        private string HtmlEncodeImpl(string input, int idxOfFirstCharWhichRequiresEncoding)
        {
            Debug.Assert(idxOfFirstCharWhichRequiresEncoding >= 0);
            Debug.Assert(idxOfFirstCharWhichRequiresEncoding < input.Length);

            // The worst case encoding is 8 output chars per input char: [input] U+FFFF -> [output] "&#xFFFF;"
            // We don't need to worry about astral code points since they consume *two* input chars to
            // generate at most 10 output chars ("&#x10FFFF;"), which equates to 5 output per input.
            int numCharsWhichMayRequireEncoding = input.Length - idxOfFirstCharWhichRequiresEncoding;
            int sbCapacity = checked(idxOfFirstCharWhichRequiresEncoding + EncoderCommon.GetCapacityOfOutputStringBuilder(numCharsWhichMayRequireEncoding, worstCaseOutputCharsPerInputChar: 8));
            Debug.Assert(sbCapacity >= input.Length);

            // Allocate the StringBuilder with the first (known to not require encoding) part of the input string,
            // then begin encoding from the last (potentially requiring encoding) part of the input string.
            StringBuilder builder = new StringBuilder(input, 0, idxOfFirstCharWhichRequiresEncoding, sbCapacity);
            fixed (char* pInput = input)
            {
                return HtmlEncodeImpl2(builder, &pInput[idxOfFirstCharWhichRequiresEncoding], (uint)numCharsWhichMayRequireEncoding);
            }
        }

        private string HtmlEncodeImpl2(StringBuilder builder, char* input, uint charsRemaining)
        {
            while (charsRemaining != 0)
            {
                int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(input, endOfString: (charsRemaining == 1));
                if (UnicodeHelpers.IsSupplementaryCodePoint(nextScalar))
                {
                    // Supplementary characters should always be encoded numerically.
                    WriteScalarAsHtmlEncodedEntity(builder, (uint)nextScalar);

                    // We consume two UTF-16 characters for a single supplementary character.
                    input += 2;
                    charsRemaining -= 2;
                }
                else
                {
                    // Otherwise, this was a BMP character.
                    input++;
                    charsRemaining--;
                    char c = (char)nextScalar;
                    if (IsCharacterAllowed(c))
                    {
                        builder.Append(c);
                    }
                    else
                    {
                        if (c == '<') { builder.Append("&lt;"); }
                        else if (c == '>') { builder.Append("&gt;"); }
                        else if (c == '&') { builder.Append("&amp;"); }
                        else if (c == '\"') { builder.Append("&quot;"); }
                        else { WriteScalarAsHtmlEncodedEntity(builder, (uint)nextScalar); }
                    }
                }
            }

            return builder.ToString();
        }

        // Determines whether the given character can be returned unencoded.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCharacterAllowed(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            return ((_allowedCharsBitmap[index] >> offset) & 0x1U) != 0;
        }

        // Writes a scalar value as "&#xFFFFFFFF;"
        private static void WriteScalarAsHtmlEncodedEntity(StringBuilder builder, uint value)
        {
            // We're building the characters up in reverse
            char* chars = stackalloc char[8 /* "FFFFFFFF" */];
            int numCharsWritten = 0;
            do
            {
                Debug.Assert(numCharsWritten < 8, "Couldn't have written 8 characters out by this point.");
                // Pop off the last nibble
                chars[numCharsWritten++] = HexUtil.IntToChar(value & 0xFU);
                value >>= 4;
            } while (value != 0);

            // Finally, write out the HTML-encoded scalar value.
            builder.Append('&');
            builder.Append('#');
            builder.Append('x');
            Debug.Assert(numCharsWritten > 0, "At least one character should've been written.");
            do
            {
                builder.Append(chars[--numCharsWritten]);
            } while (numCharsWritten != 0);
            builder.Append(';');
        }
    }
}
