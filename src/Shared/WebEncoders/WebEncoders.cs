// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if NETCOREAPP
using System.Buffers;
#endif
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.WebEncoders.Sources;

#if WebEncoders_In_WebUtilities
namespace Microsoft.AspNetCore.WebUtilities
#else
namespace Microsoft.Extensions.Internal
#endif
{
    /// <summary>
    /// Contains utility APIs to assist with common encoding and decoding operations.
    /// </summary>
#if WebEncoders_In_WebUtilities
    public
#else
    internal
#endif
    static class WebEncoders
    {
        private static readonly byte[] EmptyBytes = new byte[0];

        /// <summary>
        /// Decodes a base64url-encoded string.
        /// </summary>
        /// <param name="input">The base64url-encoded input to decode.</param>
        /// <returns>The base64url-decoded form of the input.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return Base64UrlDecode(input, offset: 0, count: input.Length);
        }

        /// <summary>
        /// Decodes a base64url-encoded substring of a given string.
        /// </summary>
        /// <param name="input">A string containing the base64url-encoded input to decode.</param>
        /// <param name="offset">The position in <paramref name="input"/> at which decoding should begin.</param>
        /// <param name="count">The number of characters in <paramref name="input"/> to decode.</param>
        /// <returns>The base64url-decoded form of the input.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input, int offset, int count)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateParameters(input.Length, nameof(input), offset, count);

            // Special-case empty input
            if (count == 0)
            {
                return EmptyBytes;
            }

            // Create array large enough for the Base64 characters, not just shorter Base64-URL-encoded form.
            var buffer = new char[GetArraySizeRequiredToDecode(count)];

            return Base64UrlDecode(input, offset, buffer, bufferOffset: 0, count: count);
        }

        /// <summary>
        /// Decodes a base64url-encoded <paramref name="input"/> into a <c>byte[]</c>.
        /// </summary>
        /// <param name="input">A string containing the base64url-encoded input to decode.</param>
        /// <param name="offset">The position in <paramref name="input"/> at which decoding should begin.</param>
        /// <param name="buffer">
        /// Scratch buffer to hold the <see cref="char"/>s to decode. Array must be large enough to hold
        /// <paramref name="bufferOffset"/> and <paramref name="count"/> characters as well as Base64 padding
        /// characters. Content is not preserved.
        /// </param>
        /// <param name="bufferOffset">
        /// The offset into <paramref name="buffer"/> at which to begin writing the <see cref="char"/>s to decode.
        /// </param>
        /// <param name="count">The number of characters in <paramref name="input"/> to decode.</param>
        /// <returns>The base64url-decoded form of the <paramref name="input"/>.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws <see cref="FormatException"/> if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input, int offset, char[] buffer, int bufferOffset, int count)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            ValidateParameters(input.Length, nameof(input), offset, count);
            if (bufferOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferOffset));
            }

            if (count == 0)
            {
                return EmptyBytes;
            }

            // Assumption: input is base64url encoded without padding and contains no whitespace.

            var paddingCharsToAdd = GetNumBase64PaddingCharsToAddForDecode(count);
            var arraySizeRequired = checked(count + paddingCharsToAdd);
            Debug.Assert(arraySizeRequired % 4 == 0, "Invariant: Array length must be a multiple of 4.");

            if (buffer.Length - bufferOffset < arraySizeRequired)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        EncoderResources.WebEncoders_InvalidCountOffsetOrLength,
                        nameof(count),
                        nameof(bufferOffset),
                        nameof(input)),
                    nameof(count));
            }

            // Copy input into buffer, fixing up '-' -> '+' and '_' -> '/'.
            var i = bufferOffset;
            for (var j = offset; i - bufferOffset < count; i++, j++)
            {
                var ch = input[j];
                if (ch == '-')
                {
                    buffer[i] = '+';
                }
                else if (ch == '_')
                {
                    buffer[i] = '/';
                }
                else
                {
                    buffer[i] = ch;
                }
            }

            // Add the padding characters back.
            for (; paddingCharsToAdd > 0; i++, paddingCharsToAdd--)
            {
                buffer[i] = '=';
            }

            // Decode.
            // If the caller provided invalid base64 chars, they'll be caught here.
            return Convert.FromBase64CharArray(buffer, bufferOffset, arraySizeRequired);
        }

        /// <summary>
        /// Gets the minimum <c>char[]</c> size required for decoding of <paramref name="count"/> characters
        /// with the <see cref="Base64UrlDecode(string, int, char[], int, int)"/> method.
        /// </summary>
        /// <param name="count">The number of characters to decode.</param>
        /// <returns>
        /// The minimum <c>char[]</c> size required for decoding  of <paramref name="count"/> characters.
        /// </returns>
        public static int GetArraySizeRequiredToDecode(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 0)
            {
                return 0;
            }

            var numPaddingCharsToAdd = GetNumBase64PaddingCharsToAddForDecode(count);

            return checked(count + numPaddingCharsToAdd);
        }

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
        public static string Base64UrlEncode(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return Base64UrlEncode(input, offset: 0, count: input.Length);
        }

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
        /// <param name="count">The number of bytes from <paramref name="input"/> to encode.</param>
        /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
        public static string Base64UrlEncode(byte[] input, int offset, int count)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateParameters(input.Length, nameof(input), offset, count);

#if NETCOREAPP
            return Base64UrlEncode(input.AsSpan(offset, count));
#else
            // Special-case empty input
            if (count == 0)
            {
                return string.Empty;
            }

            var buffer = new char[GetArraySizeRequiredToEncode(count)];
            var numBase64Chars = Base64UrlEncode(input, offset, buffer, outputOffset: 0, count: count);

            return new string(buffer, startIndex: 0, length: numBase64Chars);
#endif
        }

        /// <summary>
        /// Encodes <paramref name="input"/> using base64url encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
        /// <param name="output">
        /// Buffer to receive the base64url-encoded form of <paramref name="input"/>. Array must be large enough to
        /// hold <paramref name="outputOffset"/> characters and the full base64-encoded form of
        /// <paramref name="input"/>, including padding characters.
        /// </param>
        /// <param name="outputOffset">
        /// The offset into <paramref name="output"/> at which to begin writing the base64url-encoded form of
        /// <paramref name="input"/>.
        /// </param>
        /// <param name="count">The number of <c>byte</c>s from <paramref name="input"/> to encode.</param>
        /// <returns>
        /// The number of characters written to <paramref name="output"/>, less any padding characters.
        /// </returns>
        public static int Base64UrlEncode(byte[] input, int offset, char[] output, int outputOffset, int count)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            ValidateParameters(input.Length, nameof(input), offset, count);
            if (outputOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outputOffset));
            }

            var arraySizeRequired = GetArraySizeRequiredToEncode(count);
            if (output.Length - outputOffset < arraySizeRequired)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        EncoderResources.WebEncoders_InvalidCountOffsetOrLength,
                        nameof(count),
                        nameof(outputOffset),
                        nameof(output)),
                    nameof(count));
            }

#if NETCOREAPP
            return Base64UrlEncode(input.AsSpan(offset, count), output.AsSpan(outputOffset));
#else
            // Special-case empty input.
            if (count == 0)
            {
                return 0;
            }

            // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

            // Start with default Base64 encoding.
            var numBase64Chars = Convert.ToBase64CharArray(input, offset, count, output, outputOffset);

            // Fix up '+' -> '-' and '/' -> '_'. Drop padding characters.
            for (var i = outputOffset; i - outputOffset < numBase64Chars; i++)
            {
                var ch = output[i];
                if (ch == '+')
                {
                    output[i] = '-';
                }
                else if (ch == '/')
                {
                    output[i] = '_';
                }
                else if (ch == '=')
                {
                    // We've reached a padding character; truncate the remainder.
                    return i - outputOffset;
                }
            }

            return numBase64Chars;
#endif
        }

        /// <summary>
        /// Get the minimum output <c>char[]</c> size required for encoding <paramref name="count"/>
        /// <see cref="byte"/>s with the <see cref="Base64UrlEncode(byte[], int, char[], int, int)"/> method.
        /// </summary>
        /// <param name="count">The number of characters to encode.</param>
        /// <returns>
        /// The minimum output <c>char[]</c> size required for encoding <paramref name="count"/> <see cref="byte"/>s.
        /// </returns>
        public static int GetArraySizeRequiredToEncode(int count)
        {
            var numWholeOrPartialInputBlocks = checked(count + 2) / 3;
            return checked(numWholeOrPartialInputBlocks * 4);
        }

#if NETCOREAPP
        /// <summary>
        /// Encodes <paramref name="input"/> using base64url encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
        public static string Base64UrlEncode(ReadOnlySpan<byte> input)
        {
            if (input.IsEmpty)
            {
                return string.Empty;
            }

            int bufferSize = GetArraySizeRequiredToEncode(input.Length);

            char[] bufferToReturnToPool = null;
            Span<char> buffer = bufferSize <= 128
                ? stackalloc char[bufferSize]
                : bufferToReturnToPool = ArrayPool<char>.Shared.Rent(bufferSize);

            var numBase64Chars = Base64UrlEncode(input, buffer);
            var base64Url = new string(buffer.Slice(0, numBase64Chars));

            if (bufferToReturnToPool != null)
            {
                ArrayPool<char>.Shared.Return(bufferToReturnToPool);
            }

            return base64Url;
        }

        private static int Base64UrlEncode(ReadOnlySpan<byte> input, Span<char> output)
        {
            Debug.Assert(output.Length >= GetArraySizeRequiredToEncode(input.Length));

            if (input.IsEmpty)
            {
                return 0;
            }

            // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

            Convert.TryToBase64Chars(input, output, out int charsWritten);

            // Fix up '+' -> '-' and '/' -> '_'. Drop padding characters.
            for (var i = 0; i < charsWritten; i++)
            {
                var ch = output[i];
                if (ch == '+')
                {
                    output[i] = '-';
                }
                else if (ch == '/')
                {
                    output[i] = '_';
                }
                else if (ch == '=')
                {
                    // We've reached a padding character; truncate the remainder.
                    return i;
                }
            }

            return charsWritten;
        }
#endif

        private static int GetNumBase64PaddingCharsInString(string str)
        {
            // Assumption: input contains a well-formed base64 string with no whitespace.

            // base64 guaranteed have 0 - 2 padding characters.
            if (str[str.Length - 1] == '=')
            {
                if (str[str.Length - 2] == '=')
                {
                    return 2;
                }
                return 1;
            }
            return 0;
        }

        private static int GetNumBase64PaddingCharsToAddForDecode(int inputLength)
        {
            switch (inputLength % 4)
            {
                case 0:
                    return 0;
                case 2:
                    return 2;
                case 3:
                    return 1;
                default:
                    throw new FormatException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            EncoderResources.WebEncoders_MalformedInput,
                            inputLength));
            }
        }

        private static void ValidateParameters(int bufferLength, string inputName, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (bufferLength - offset < count)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        EncoderResources.WebEncoders_InvalidCountOffsetOrLength,
                        nameof(count),
                        nameof(offset),
                        inputName),
                    nameof(count));
            }
        }
    }
}
