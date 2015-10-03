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
        /// Instantiates an encoder using <see cref="UnicodeRanges.BasicLatin"/> as its allow list.
        /// Any character not in the <see cref="UnicodeRanges.BasicLatin"/> range will be escaped.
        /// </summary>
        public UrlEncoder()
            : this(UrlUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder specifying which Unicode character ranges are allowed to
        /// pass through the encoder unescaped. Any character not in the set of ranges specified
        /// by <paramref name="allowedRanges"/> will be escaped.
        /// </summary>
        public UrlEncoder(params UnicodeRange[] allowedRanges)
            : this(new UrlUnicodeEncoder(new CodePointFilter(allowedRanges)))
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom code point filter. Any character not in the
        /// set returned by <paramref name="filter"/>'s <see cref="ICodePointFilter.GetAllowedCodePoints"/>
        /// method will be escaped.
        /// </summary>
        public UrlEncoder(ICodePointFilter filter)
            : this(new UrlUnicodeEncoder(CodePointFilter.Wrap(filter)))
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
        }

        private UrlEncoder(UrlUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
        }

        /// <summary>
        /// A default instance of <see cref="UrlEncoder"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static UrlEncoder Default
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
        private static UrlEncoder CreateDefaultEncoderSlow()
        {
            var onDemandEncoder = new UrlEncoder();
            return Interlocked.CompareExchange(ref _defaultEncoder, onDemandEncoder, null) ?? onDemandEncoder;
        }

        /// <summary>
        /// Everybody's favorite UrlEncode routine.
        /// </summary>
        public void UrlEncode(char[] value, int startIndex, int charCount, TextWriter output)
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
                // four particular components: 'isegment', 'ipath-noscheme',
                // 'iquery', and 'ifragment'. The relevant definitions are below.
                //
                //    ipath-noscheme = isegment-nz-nc *( "/" isegment )
                // 
                //    isegment       = *ipchar
                // 
                //    isegment-nz-nc = 1*( iunreserved / pct-encoded / sub-delims
                //                         / "@" )
                //                   ; non-zero-length segment without any colon ":"
                //
                //    ipchar         = iunreserved / pct-encoded / sub-delims / ":"
                //                   / "@"
                // 
                //    iquery         = *( ipchar / iprivate / "/" / "?" )
                // 
                //    ifragment      = *( ipchar / "/" / "?" )
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
                // The only common characters between these four components are the
                // intersection of 'isegment-nz-nc' and 'ipchar', which is really
                // just 'isegment-nz-nc' (colons forbidden).
                // 
                // From this list, the base encoder already forbids "&", "'", "+",
                // and we'll additionally forbid "=" since it has special meaning
                // in x-www-form-urlencoded representations.
                //
                // This means that the full list of allowed characters from the
                // Basic Latin set is:
                // ALPHA / DIGIT / "-" / "." / "_" / "~" / "!" / "$" / "(" / ")" / "*" / "," / ";" / "@"

                const string forbiddenChars = @" #%/:=?[\]^`{|}"; // chars from Basic Latin which aren't already disallowed by the base encoder
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
                        encoder = new UrlUnicodeEncoder(new CodePointFilter(UnicodeRanges.BasicLatin));
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
