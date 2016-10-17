// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Channels;

namespace Microsoft.Extensions.WebSockets.Internal
{
    /// <summary>
    /// Stateful UTF-8 validator.
    /// </summary>
    public class Utf8Validator
    {
        // Table of UTF-8 code point widths. '0' indicates an invalid first byte.
        private static readonly byte[] _utf8Width = new byte[256]
        {
            /* 0x00 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x0F */
            /* 0x10 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x1F */
            /* 0x20 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x2F */
            /* 0x30 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x3F */
            /* 0x40 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x4F */
            /* 0x50 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x5F */
            /* 0x60 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x6F */
            /* 0x70 */ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, /* 0x7F */
            /* 0x80 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, /* 0x8F */
            /* 0x90 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, /* 0x9F */
            /* 0xA0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, /* 0xAF */
            /* 0xB0 */ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, /* 0xBF */
            /* 0xC0 */ 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, /* 0xCF */
            /* 0xD0 */ 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, /* 0xDF */
            /* 0xE0 */ 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, /* 0xEF */
            /* 0xF0 */ 4, 4, 4, 4, 4, 4, 4, 4, 4, 0, 0, 0, 0, 0, 0, 0, /* 0xFF */
        };

        // Table of masks used to extract the code point bits from the first byte. Indexed by (width - 1)
        private static readonly byte[] _utf8Mask = new byte[4] { 0x7F, 0x1F, 0x0F, 0x07 };

        // Table of minimum valid code-points based on the width. Indexed by (width - 1)
        private static readonly int[] _utf8Min = new int[4] { 0x00000, 0x00080, 0x00800, 0x10000 };

        private struct Utf8ValidatorState
        {
            public bool _withinSequence;
            public int _remainingBytesInChar;
            public int _currentDecodedValue;
            public int _minCodePoint;

            public void Reset()
            {
                _withinSequence = false;
                _remainingBytesInChar = 0;
                _currentDecodedValue = 0;
                _minCodePoint = 0;
            }
        }

        private Utf8ValidatorState _state;

        public void Reset()
        {
            _state.Reset();
        }

        public bool ValidateUtf8Frame(ReadableBuffer payload, bool fin) => ValidateUtf8(ref _state, payload, fin);

        public static bool ValidateUtf8(ReadableBuffer payload)
        {
            var state = new Utf8ValidatorState();
            return ValidateUtf8(ref state, payload, fin: true);
        }

        private static bool ValidateUtf8(ref Utf8ValidatorState state, ReadableBuffer payload, bool fin)
        {
            // Walk through the payload verifying it
            var offset = 0;
            foreach (var mem in payload)
            {
                var span = mem.Span;
                for (int i = 0; i < span.Length; i++)
                {
                    var b = span[i];
                    if (!state._withinSequence)
                    {
                        // This is the first byte of a char, so set things up
                        var width = _utf8Width[b];
                        state._remainingBytesInChar = width - 1;
                        if (state._remainingBytesInChar < 0)
                        {
                            // Invalid first byte
                            return false;
                        }

                        // Use the width (-1) to index into the mask and min tables.
                        state._currentDecodedValue = b & _utf8Mask[width - 1];
                        state._minCodePoint = _utf8Min[width - 1];
                        state._withinSequence = true;
                    }
                    else
                    {
                        // Add this byte to the value
                        state._currentDecodedValue = (state._currentDecodedValue << 6) | (b & 0x3F);
                        state._remainingBytesInChar--;
                    }

                    // Fast invalid exits
                    if (state._remainingBytesInChar == 1 && state._currentDecodedValue >= 0x360 && state._currentDecodedValue <= 0x37F)
                    {
                        // This will be a UTF-16 surrogate: 0xD800-0xDFFF
                        return false;
                    }
                    if (state._remainingBytesInChar == 2 && state._currentDecodedValue >= 0x110)
                    {
                        // This will be above the maximum Unicode character (0x10FFFF).
                        return false;
                    }

                    if (state._remainingBytesInChar == 0)
                    {
                        // Check the range of the final decoded value
                        if (state._currentDecodedValue < state._minCodePoint)
                        {
                            // This encoding is longer than it should be, which is not allowed.
                            return false;
                        }

                        // Reset state
                        state._withinSequence = false;
                    }
                    offset++;
                }
            }

            // We're done.
            // The value is valid if:
            //  1. We haven't reached the end of the whole message yet (we'll be caching this state for the next message)
            //  2. We aren't inside a character sequence (i.e. the last character isn't unterminated)
            return !fin || !state._withinSequence;
        }
    }
}
