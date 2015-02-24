// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// A class which can perform URL string escaping given an allow list of characters which
    /// can be represented unescaped.
    /// </summary>
    /// <remarks>
    /// Instances of this type will always encode a certain set of characters (such as +
    /// and ?), even if the filter provided in the constructor allows such characters.
    /// Once constructed, instances of this class are thread-safe for multiple callers.
    /// </remarks>
    public sealed class UrlEncoder : IUrlEncoder
    {
        // The default URL string encoder (Basic Latin), instantiated on demand
        private static UrlEncoder _defaultEncoder;

        // The inner encoder, responsible for the actual encoding routines
        private readonly UrlUnicodeEncoder _innerUnicodeEncoder;

        /// <summary>
        /// Instantiates an encoder using the 'Basic Latin' code table as the allow list.
        /// </summary>
        public UrlEncoder()
            : this(UrlUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder specifying which Unicode character blocks are allowed to
        /// pass through the encoder unescaped.
        /// </summary>
        public UrlEncoder(params UnicodeBlock[] allowedBlocks)
            : this(new UrlUnicodeEncoder(new CodePointFilter(allowedBlocks)))
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom code point filter.
        /// </summary>
        public UrlEncoder(ICodePointFilter filter)
            : this(new UrlUnicodeEncoder(CodePointFilter.Wrap(filter)))
        {
        }

        private UrlEncoder(UrlUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
        }

        /// <summary>
        /// A default instance of the UrlEncoder, equivalent to allowing only
        /// the 'Basic Latin' character range.
        /// </summary>
        public static UrlEncoder Default
        {
            get
            {
                UrlEncoder defaultEncoder = Volatile.Read(ref _defaultEncoder);
                if (defaultEncoder == null)
                {
                    defaultEncoder = new UrlEncoder();
                    Volatile.Write(ref _defaultEncoder, defaultEncoder);
                }
                return defaultEncoder;
            }
        }

        /// <summary>
        /// Everybody's favorite UrlEncode routine.
        /// </summary>
        public void UrlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, charCount, output);
        }

        /// <summary>
        /// Everybody's favorite UrlEncode routine.
        /// </summary>
        public string UrlEncode(string value)
        {
            return _innerUnicodeEncoder.Encode(value);
        }

        /// <summary>
        /// Everybody's favorite UrlEncode routine.
        /// </summary>
        public void UrlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, charCount, output);
        }

        private sealed class UrlUnicodeEncoder : UnicodeEncoderBase
        {
            // A singleton instance of the basic latin encoder.
            private static UrlUnicodeEncoder _basicLatinSingleton;

            // We perform UTF8 conversion of input, which means that the worst case is
            // 9 output chars per input char: [input] U+FFFF -> [output] "%XX%YY%ZZ".
            // We don't need to worry about astral code points since they consume 2 input
            // chars to produce 12 output chars "%XX%YY%ZZ%WW", which is 6 output chars per input char.
            private const int MaxOutputCharsPerInputChar = 9;

            internal UrlUnicodeEncoder(CodePointFilter filter)
                : base(filter, MaxOutputCharsPerInputChar)
            {
                // Per RFC 3987, Sec. 2.2, we want encodings that are safe for
                // 'isegment', 'iquery', and 'ifragment'. The only thing these
                // all have in common is 'ipchar', which is defined as such:
                //
                //    ipchar         = iunreserved / pct-encoded / sub-delims / ":"
                //                   / "@"
                // 
                //    iunreserved    = ALPHA / DIGIT / "-" / "." / "_" / "~" / ucschar
                // 
                //    ucschar        = %xA0-D7FF / %xF900-FDCF / %xFDF0-FFEF
                //                   / %x10000-1FFFD / %x20000-2FFFD / %x30000-3FFFD
                //                   / %x40000-4FFFD / %x50000-5FFFD / %x60000-6FFFD
                //                   / %x70000-7FFFD / %x80000-8FFFD / %x90000-9FFFD
                //                   / %xA0000-AFFFD / %xB0000-BFFFD / %xC0000-CFFFD
                //                   / %xD0000-DFFFD / %xE1000-EFFFD
                // 
                //    pct-encoded    = "%" HEXDIG HEXDIG
                // 
                //    sub-delims     = "!" / "$" / "&" / "'" / "(" / ")"
                //                   / "*" / "+" / "," / ";" / "="
                //
                // From this list, the base encoder blocks "&", "'", "+",
                // and we'll additionally block "=" since it has special meaning
                // in x-www-form-urlencoded representations.
                //
                // This means that the full list of allowed characters from the
                // Basic Latin set is:
                // ALPHA / DIGIT / "-" / "." / "_" / "~" / "!" / "$" / "(" / ")" / "*" / "," / ";" / ":" / "@"

                const string forbiddenChars = @" #%/=?[\]^`{|}"; // chars from Basic Latin which aren't already disallowed by the base encoder
                foreach (char c in forbiddenChars)
                {
                    ForbidCharacter(c);
                }

                // Specials (U+FFF0 .. U+FFFF) are forbidden by the definition of 'ucschar' above
                for (int i = 0; i < 16; i++)
                {
                    ForbidCharacter((char)(0xFFF0 | i));
                }

                // Supplementary characters are forbidden anyway by the base encoder
            }

            internal static UrlUnicodeEncoder BasicLatin
            {
                get
                {
                    UrlUnicodeEncoder encoder = Volatile.Read(ref _basicLatinSingleton);
                    if (encoder == null)
                    {
                        encoder = new UrlUnicodeEncoder(new CodePointFilter());
                        Volatile.Write(ref _basicLatinSingleton, encoder);
                    }
                    return encoder;
                }
            }

            // Writes a scalar value as a percent-encoded sequence of UTF8 bytes, per RFC 3987.
            protected override void WriteEncodedScalar(ref Writer writer, uint value)
            {
                uint asUtf8 = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue(value);
                do
                {
                    char highNibble, lowNibble;
                    HexUtil.WriteHexEncodedByte((byte)asUtf8, out highNibble, out lowNibble);
                    writer.Write('%');
                    writer.Write(highNibble);
                    writer.Write(lowNibble);
                } while ((asUtf8 >>= 8) != 0);
            }
        }
    }
}
