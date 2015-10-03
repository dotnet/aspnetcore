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
    /// A class which can perform JavaScript string escaping given an allow list of characters which
    /// can be represented unescaped.
    /// </summary>
    /// <remarks>
    /// Instances of this type will always encode a certain set of characters (such as '
    /// and "), even if the filter provided in the constructor allows such characters.
    /// Once constructed, instances of this class are thread-safe for multiple callers.
    /// </remarks>
    public sealed class JavaScriptStringEncoder : IJavaScriptStringEncoder
    {
        // The default JavaScript string encoder (Basic Latin), instantiated on demand
        private static JavaScriptStringEncoder _defaultEncoder;

        // The inner encoder, responsible for the actual encoding routines
        private readonly JavaScriptStringUnicodeEncoder _innerUnicodeEncoder;

        /// <summary>
        /// Instantiates an encoder using <see cref="UnicodeRanges.BasicLatin"/> as its allow list.
        /// Any character not in the <see cref="UnicodeRanges.BasicLatin"/> range will be escaped.
        /// </summary>
        public JavaScriptStringEncoder()
            : this(JavaScriptStringUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder specifying which Unicode character ranges are allowed to
        /// pass through the encoder unescaped. Any character not in the set of ranges specified
        /// by <paramref name="allowedRanges"/> will be escaped.
        /// </summary>
        public JavaScriptStringEncoder(params UnicodeRange[] allowedRanges)
            : this(new JavaScriptStringUnicodeEncoder(new CodePointFilter(allowedRanges)))
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom code point filter. Any character not in the
        /// set returned by <paramref name="filter"/>'s <see cref="ICodePointFilter.GetAllowedCodePoints"/>
        /// method will be escaped.
        /// </summary>
        public JavaScriptStringEncoder(ICodePointFilter filter)
            : this(new JavaScriptStringUnicodeEncoder(CodePointFilter.Wrap(filter)))
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }
        }

        private JavaScriptStringEncoder(JavaScriptStringUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
        }

        /// <summary>
        /// A default instance of <see cref="JavaScriptStringEncoder"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static JavaScriptStringEncoder Default
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
        private static JavaScriptStringEncoder CreateDefaultEncoderSlow()
        {
            var onDemandEncoder = new JavaScriptStringEncoder();
            return Interlocked.CompareExchange(ref _defaultEncoder, onDemandEncoder, null) ?? onDemandEncoder;
        }

        /// <summary>
        /// Everybody's favorite JavaScriptStringEncode routine.
        /// </summary>
        public void JavaScriptStringEncode(char[] value, int startIndex, int charCount, TextWriter output)
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
        /// Everybody's favorite JavaScriptStringEncode routine.
        /// </summary>
        public string JavaScriptStringEncode(string value)
        {
            return _innerUnicodeEncoder.Encode(value);
        }

        /// <summary>
        /// Everybody's favorite JavaScriptStringEncode routine.
        /// </summary>
        public void JavaScriptStringEncode(string value, int startIndex, int charCount, TextWriter output)
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

        private sealed class JavaScriptStringUnicodeEncoder : UnicodeEncoderBase
        {
            // A singleton instance of the basic latin encoder.
            private static JavaScriptStringUnicodeEncoder _basicLatinSingleton;

            // The worst case encoding is 6 output chars per input char: [input] U+FFFF -> [output] "\uFFFF"
            // We don't need to worry about astral code points since they're represented as encoded
            // surrogate pairs in the output.
            private const int MaxOutputCharsPerInputChar = 6;

            internal JavaScriptStringUnicodeEncoder(CodePointFilter filter)
                : base(filter, MaxOutputCharsPerInputChar)
            {
                // The only interesting characters above and beyond what the base encoder
                // already covers are the solidus and reverse solidus.
                ForbidCharacter('\\');
                ForbidCharacter('/');
            }

            internal static JavaScriptStringUnicodeEncoder BasicLatin
            {
                get
                {
                    JavaScriptStringUnicodeEncoder encoder = Volatile.Read(ref _basicLatinSingleton);
                    if (encoder == null)
                    {
                        encoder = new JavaScriptStringUnicodeEncoder(new CodePointFilter(UnicodeRanges.BasicLatin));
                        Volatile.Write(ref _basicLatinSingleton, encoder);
                    }
                    return encoder;
                }
            }

            // Writes a scalar value as a JavaScript-escaped character (or sequence of characters).
            // See ECMA-262, Sec. 7.8.4, and ECMA-404, Sec. 9
            // http://www.ecma-international.org/ecma-262/5.1/#sec-7.8.4
            // http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf
            protected override void WriteEncodedScalar(ref Writer writer, uint value)
            {
                // ECMA-262 allows encoding U+000B as "\v", but ECMA-404 does not.
                // Both ECMA-262 and ECMA-404 allow encoding U+002F SOLIDUS as "\/".
                // (In ECMA-262 this character is a NonEscape character.)
                // HTML-specific characters (including apostrophe and quotes) will
                // be written out as numeric entities for defense-in-depth.
                // See UnicodeEncoderBase ctor comments for more info.

                if (value == (uint)'\b') { writer.Write(@"\b"); }
                else if (value == (uint)'\t') { writer.Write(@"\t"); }
                else if (value == (uint)'\n') { writer.Write(@"\n"); }
                else if (value == (uint)'\f') { writer.Write(@"\f"); }
                else if (value == (uint)'\r') { writer.Write(@"\r"); }
                else if (value == (uint)'/') { writer.Write(@"\/"); }
                else if (value == (uint)'\\') { writer.Write(@"\\"); }
                else { WriteEncodedScalarAsNumericEntity(ref writer, value); }
            }

            // Writes a scalar value as an JavaScript-escaped character (or sequence of characters).
            private static void WriteEncodedScalarAsNumericEntity(ref Writer writer, uint value)
            {
                if (UnicodeHelpers.IsSupplementaryCodePoint((int)value))
                {
                    // Convert this back to UTF-16 and write out both characters.
                    char leadingSurrogate, trailingSurrogate;
                    UnicodeHelpers.GetUtf16SurrogatePairFromAstralScalarValue((int)value, out leadingSurrogate, out trailingSurrogate);
                    WriteEncodedSingleCharacter(ref writer, leadingSurrogate);
                    WriteEncodedSingleCharacter(ref writer, trailingSurrogate);
                }
                else
                {
                    // This is only a single character.
                    WriteEncodedSingleCharacter(ref writer, value);
                }
            }

            // Writes an encoded scalar value (in the BMP) as a JavaScript-escaped character.
            private static void WriteEncodedSingleCharacter(ref Writer writer, uint value)
            {
                Debug.Assert(!UnicodeHelpers.IsSupplementaryCodePoint((int)value), "The incoming value should've been in the BMP.");

                // Encode this as 6 chars "\uFFFF".
                writer.Write('\\');
                writer.Write('u');
                writer.Write(HexUtil.IntToChar(value >> 12));
                writer.Write(HexUtil.IntToChar((value >> 8) & 0xFU));
                writer.Write(HexUtil.IntToChar((value >> 4) & 0xFU));
                writer.Write(HexUtil.IntToChar(value & 0xFU));
            }
        }
    }
}
