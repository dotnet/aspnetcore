// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// A class which can perform HTML encoding given an allow list of characters which
    /// can be represented unencoded.
    /// </summary>
    /// <remarks>
    /// Instances of this type will always encode a certain set of characters (such as &lt;
    /// and &gt;), even if the filter provided in the constructor allows such characters.
    /// Once constructed, instances of this class are thread-safe for multiple callers.
    /// </remarks>
    public unsafe sealed class HtmlEncoder : IHtmlEncoder
    {
        // The default HtmlEncoder (Basic Latin), instantiated on demand
        private static HtmlEncoder _defaultEncoder;

        // The inner encoder, responsible for the actual encoding routines
        private readonly HtmlUnicodeEncoder _innerUnicodeEncoder;

        /// <summary>
        /// Instantiates an encoder using <see cref="UnicodeRanges.BasicLatin"/> as its allow list.
        /// Any character not in the <see cref="UnicodeRanges.BasicLatin"/> range will be escaped.
        /// </summary>
        public HtmlEncoder()
            : this(HtmlUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder specifying which Unicode character ranges are allowed to
        /// pass through the encoder unescaped. Any character not in the set of ranges specified
        /// by <paramref name="allowedRanges"/> will be escaped.
        /// </summary>
        public HtmlEncoder(params UnicodeRange[] allowedRanges)
            : this(new HtmlUnicodeEncoder(new CodePointFilter(allowedRanges)))
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom code point filter. Any character not in the
        /// set returned by <paramref name="filter"/>'s <see cref="ICodePointFilter.GetAllowedCodePoints"/>
        /// method will be escaped.
        /// </summary>
        public HtmlEncoder(ICodePointFilter filter)
            : this(new HtmlUnicodeEncoder(CodePointFilter.Wrap(filter)))
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
        }

        private HtmlEncoder(HtmlUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
        }

        /// <summary>
        /// A default instance of <see cref="HtmlEncoder"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static HtmlEncoder Default
        {
            get
            {
                return Volatile.Read(ref _defaultEncoder) ?? CreateDefaultEncoderSlow();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Volatile.Write(ref _defaultEncoder, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the JITter can attempt to inline the caller itself without worrying about us
        private static HtmlEncoder CreateDefaultEncoderSlow()
        {
            var onDemandEncoder = new HtmlEncoder();
            return Interlocked.CompareExchange(ref _defaultEncoder, onDemandEncoder, null) ?? onDemandEncoder;
        }

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            _innerUnicodeEncoder.Encode(value, startIndex, charCount, output);
        }

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public string HtmlEncode(string value)
        {
            return _innerUnicodeEncoder.Encode(value);
        }

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public void HtmlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            _innerUnicodeEncoder.Encode(value, startIndex, charCount, output);
        }

        private sealed class HtmlUnicodeEncoder : UnicodeEncoderBase
        {
            // A singleton instance of the basic latin encoder.
            private static HtmlUnicodeEncoder _basicLatinSingleton;

            // The worst case encoding is 8 output chars per input char: [input] U+FFFF -> [output] "&#xFFFF;"
            // We don't need to worry about astral code points since they consume *two* input chars to
            // generate at most 10 output chars ("&#x10FFFF;"), which equates to 5 output chars per input char.
            private const int MaxOutputCharsPerInputChar = 8;

            internal HtmlUnicodeEncoder(CodePointFilter filter)
                : base(filter, MaxOutputCharsPerInputChar)
            {
            }

            internal static HtmlUnicodeEncoder BasicLatin
            {
                get
                {
                    HtmlUnicodeEncoder encoder = Volatile.Read(ref _basicLatinSingleton);
                    if (encoder == null)
                    {
                        encoder = new HtmlUnicodeEncoder(new CodePointFilter(UnicodeRanges.BasicLatin));
                        Volatile.Write(ref _basicLatinSingleton, encoder);
                    }
                    return encoder;
                }
            }

            // Writes a scalar value as an HTML-encoded entity.
            protected override void WriteEncodedScalar(ref Writer writer, uint value)
            {
                if (value == (uint)'\"') { writer.Write("&quot;"); }
                else if (value == (uint)'&') { writer.Write("&amp;"); }
                else if (value == (uint)'<') { writer.Write("&lt;"); }
                else if (value == (uint)'>') { writer.Write("&gt;"); }
                else { WriteEncodedScalarAsNumericEntity(ref writer, value); }
            }

            // Writes a scalar value as an HTML-encoded numeric entity.
            private static void WriteEncodedScalarAsNumericEntity(ref Writer writer, uint value)
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
                writer.Write('&');
                writer.Write('#');
                writer.Write('x');
                Debug.Assert(numCharsWritten > 0, "At least one character should've been written.");
                do
                {
                    writer.Write(chars[--numCharsWritten]);
                } while (numCharsWritten != 0);
                writer.Write(';');
            }
        }
    }
}
