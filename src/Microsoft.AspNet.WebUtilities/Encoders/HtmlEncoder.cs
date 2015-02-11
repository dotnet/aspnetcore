// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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

        // The inner encoder, responsible for the actual encoding routines
        private readonly HtmlUnicodeEncoder _innerUnicodeEncoder;

        /// <summary>
        /// Instantiates an encoder using the 'Basic Latin' code table as the allow list.
        /// </summary>
        public HtmlEncoder()
            : this(HtmlUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom allow list of characters.
        /// </summary>
        public HtmlEncoder(params ICodePointFilter[] filters)
            : this(new HtmlUnicodeEncoder(filters))
        {
        }

        private HtmlEncoder(HtmlUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
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

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public string HtmlEncode(string value)
        {
            return _innerUnicodeEncoder.Encode(value);
        }

        private sealed class HtmlUnicodeEncoder : UnicodeEncoderBase
        {
            // A singleton instance of the basic latin encoder.
            private static HtmlUnicodeEncoder _basicLatinSingleton;

            // The worst case encoding is 8 output chars per input char: [input] U+FFFF -> [output] "&#xFFFF;"
            // We don't need to worry about astral code points since they consume *two* input chars to
            // generate at most 10 output chars ("&#x10FFFF;"), which equates to 5 output chars per input char.
            private const int MaxOutputCharsPerInputChar = 8;

            internal HtmlUnicodeEncoder(ICodePointFilter[] filters)
                : base(filters, MaxOutputCharsPerInputChar)
            {
            }

            internal static HtmlUnicodeEncoder BasicLatin
            {
                get
                {
                    HtmlUnicodeEncoder encoder = Volatile.Read(ref _basicLatinSingleton);
                    if (encoder == null)
                    {
                        encoder = new HtmlUnicodeEncoder(new[] { CodePointFilters.BasicLatin });
                        Volatile.Write(ref _basicLatinSingleton, encoder);
                    }
                    return encoder;
                }
            }

            // Writes a scalar value as an HTML-encoded entity.
            protected override void WriteEncodedScalar(StringBuilder builder, uint value)
            {
                if (value == (uint)'\"') { builder.Append("&quot;"); }
                else if (value == (uint)'&') { builder.Append("&amp;"); }
                else if (value == (uint)'<') { builder.Append("&lt;"); }
                else if (value == (uint)'>') { builder.Append("&gt;"); }
                else { WriteEncodedScalarAsNumericEntity(builder, value); }
            }

            // Writes a scalar value as an HTML-encoded numeric entity.
            private static void WriteEncodedScalarAsNumericEntity(StringBuilder builder, uint value)
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
}
