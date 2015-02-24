// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Framework.WebEncoders
{
    internal unsafe abstract class UnicodeEncoderBase
    {
        // A bitmap of characters which are allowed to be returned unescaped.
        private AllowedCharsBitmap _allowedCharsBitmap;

        // The worst-case number of output chars generated for any input char.
        private readonly int _maxOutputCharsPerInputChar;

        /// <summary>
        /// Instantiates an encoder using a custom allow list of characters.
        /// </summary>
        protected UnicodeEncoderBase(CodePointFilter filter, int maxOutputCharsPerInputChar)
        {
            _maxOutputCharsPerInputChar = maxOutputCharsPerInputChar;
            _allowedCharsBitmap = filter.GetAllowedCharsBitmap();

            // Forbid characters that are special in HTML.
            // Even though this is a common encoder used by everybody (including URL
            // and JavaScript strings), it's unfortunately common for developers to
            // forget to HTML-encode a string once it has been URL-encoded or
            // JavaScript string-escaped, so this offers extra protection.
            ForbidCharacter('<');
            ForbidCharacter('>');
            ForbidCharacter('&');
            ForbidCharacter('\''); // can be used to escape attributes
            ForbidCharacter('\"'); // can be used to escape attributes
            ForbidCharacter('+'); // technically not HTML-specific, but can be used to perform UTF7-based attacks

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            _allowedCharsBitmap.ForbidUndefinedCharacters();
        }

        // Marks a character as forbidden (must be returned encoded)
        protected void ForbidCharacter(char c)
        {
            _allowedCharsBitmap.ForbidCharacter(c);
        }
        
        /// <summary>
        /// Entry point to the encoder.
        /// </summary>
        public void Encode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            // Input checking
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            ValidateInputs(startIndex, charCount, actualInputLength: value.Length);

            if (charCount != 0)
            {
                fixed (char* pChars = value)
                {
                    int indexOfFirstCharWhichRequiresEncoding = GetIndexOfFirstCharWhichRequiresEncoding(&pChars[startIndex], charCount);
                    if (indexOfFirstCharWhichRequiresEncoding < 0)
                    {
                        // All chars are valid - just copy the buffer as-is.
                        output.Write(value, startIndex, charCount);
                    }
                    else
                    {
                        // Flush all chars which are known to be valid, then encode the remainder individually
                        if (indexOfFirstCharWhichRequiresEncoding > 0)
                        {
                            output.Write(value, startIndex, indexOfFirstCharWhichRequiresEncoding);
                        }
                        EncodeCore(&pChars[startIndex + indexOfFirstCharWhichRequiresEncoding], (uint)(charCount - indexOfFirstCharWhichRequiresEncoding), output);
                    }
                }
            }
        }

        /// <summary>
        /// Entry point to the encoder.
        /// </summary>
        public string Encode(string value)
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
                    return EncodeCore(value, idxOfFirstCharWhichRequiresEncoding: i);
                }
            }
            return value;
        }

        /// <summary>
        /// Entry point to the encoder.
        /// </summary>
        public void Encode(string value, int startIndex, int charCount, TextWriter output)
        {
            // Input checking
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            ValidateInputs(startIndex, charCount, actualInputLength: value.Length);

            if (charCount != 0)
            {
                fixed (char* pChars = value)
                {
                    if (charCount == value.Length)
                    {
                        // Optimize for the common case: we're being asked to encode the entire input string
                        // (not just a subset). If all characters are safe, we can just spit it out as-is.
                        int indexOfFirstCharWhichRequiresEncoding = GetIndexOfFirstCharWhichRequiresEncoding(pChars, charCount);
                        if (indexOfFirstCharWhichRequiresEncoding < 0)
                        {
                            output.Write(value);
                        }
                        else
                        {
                            // Flush all chars which are known to be valid, then encode the remainder individually
                            for (int i = 0; i < indexOfFirstCharWhichRequiresEncoding; i++)
                            {
                                output.Write(pChars[i]);
                            }
                            EncodeCore(&pChars[indexOfFirstCharWhichRequiresEncoding], (uint)(charCount - indexOfFirstCharWhichRequiresEncoding), output);
                        }
                    }
                    else
                    {
                        // We're being asked to encode a subset, so we need to go through the slow path of appending
                        // each character individually.
                        EncodeCore(&pChars[startIndex], (uint)charCount, output);
                    }
                }
            }
        }

        private string EncodeCore(string input, int idxOfFirstCharWhichRequiresEncoding)
        {
            Debug.Assert(idxOfFirstCharWhichRequiresEncoding >= 0);
            Debug.Assert(idxOfFirstCharWhichRequiresEncoding < input.Length);

            int numCharsWhichMayRequireEncoding = input.Length - idxOfFirstCharWhichRequiresEncoding;
            int sbCapacity = checked(idxOfFirstCharWhichRequiresEncoding + EncoderCommon.GetCapacityOfOutputStringBuilder(numCharsWhichMayRequireEncoding, _maxOutputCharsPerInputChar));
            Debug.Assert(sbCapacity >= input.Length);

            // Allocate the StringBuilder with the first (known to not require encoding) part of the input string,
            // then begin encoding from the last (potentially requiring encoding) part of the input string.
            StringBuilder builder = new StringBuilder(input, 0, idxOfFirstCharWhichRequiresEncoding, sbCapacity);
            Writer writer = new Writer(builder);
            fixed (char* pInput = input)
            {
                EncodeCore(ref writer, &pInput[idxOfFirstCharWhichRequiresEncoding], (uint)numCharsWhichMayRequireEncoding);
            }
            return builder.ToString();
        }

        private void EncodeCore(char* input, uint charsRemaining, TextWriter output)
        {
            Writer writer = new Writer(output);
            EncodeCore(ref writer, input, charsRemaining);
        }

        private void EncodeCore(ref Writer writer, char* input, uint charsRemaining)
        {
            while (charsRemaining != 0)
            {
                int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(input, endOfString: (charsRemaining == 1));
                if (UnicodeHelpers.IsSupplementaryCodePoint(nextScalar))
                {
                    // Supplementary characters should always be encoded numerically.
                    WriteEncodedScalar(ref writer, (uint)nextScalar);

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
                        writer.Write(c);
                    }
                    else
                    {
                        WriteEncodedScalar(ref writer, (uint)nextScalar);
                    }
                }
            }
        }

        private int GetIndexOfFirstCharWhichRequiresEncoding(char* input, int inputLength)
        {
            for (int i = 0; i < inputLength; i++)
            {
                if (!IsCharacterAllowed(input[i]))
                {
                    return i;
                }
            }
            return -1; // no characters require encoding
        }

        // Determines whether the given character can be returned unencoded.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCharacterAllowed(char c)
        {
            return _allowedCharsBitmap.IsCharacterAllowed(c);
        }

        private static void ValidateInputs(int startIndex, int charCount, int actualInputLength)
        {
            if (startIndex < 0 || startIndex > actualInputLength)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
            if (charCount < 0 || charCount > (actualInputLength - startIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(charCount));
            }
        }

        protected abstract void WriteEncodedScalar(ref Writer writer, uint value);

        /// <summary>
        /// Provides an abstraction over both StringBuilder and TextWriter.
        /// Declared as a struct so we can allocate on the stack and pass by
        /// reference. Eliminates chatty virtual dispatches on hot paths.
        /// </summary>
        protected struct Writer
        {
            private readonly StringBuilder _innerBuilder;
            private readonly TextWriter _innerWriter;

            public Writer(StringBuilder innerBuilder)
            {
                _innerBuilder = innerBuilder;
                _innerWriter = null;
            }

            public Writer(TextWriter innerWriter)
            {
                _innerBuilder = null;
                _innerWriter = innerWriter;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write(char value)
            {
                if (_innerBuilder != null)
                {
                    _innerBuilder.Append(value);
                }
                else
                {
                    _innerWriter.Write(value);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write(string value)
            {
                if (_innerBuilder != null)
                {
                    _innerBuilder.Append(value);
                }
                else
                {
                    _innerWriter.Write(value);
                }
            }
        }
    }
}
