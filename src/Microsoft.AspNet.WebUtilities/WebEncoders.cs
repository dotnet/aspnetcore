// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.WebUtilities
{
    /// <summary>
    /// Contains utility APIs to assist with common encoding and decoding operations.
    /// </summary>
    public static class WebEncoders
    {
        /// <summary>
        /// Decodes a base64url-encoded string.
        /// </summary>
        /// <param name="input">The base64url-encoded input to decode.</param>
        /// <returns>The base64url-decoded form of the input.</returns>
        /// <remarks>
        /// The input must not contain any whitespace or padding characters.
        /// Throws FormatException if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return Base64UrlDecode(input, 0, input.Length);
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
        /// Throws FormatException if the input is malformed.
        /// </remarks>
        public static byte[] Base64UrlDecode(string input, int offset, int count)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateParameters(input.Length, offset, count);

            // Special-case empty input
            if (count == 0)
            {
                return new byte[0];
            }

            // Assumption: input is base64url encoded without padding and contains no whitespace.

            // First, we need to add the padding characters back.
            int numPaddingCharsToAdd = GetNumBase64PaddingCharsToAddForDecode(count);
            char[] completeBase64Array = new char[checked(count + numPaddingCharsToAdd)];
            Debug.Assert(completeBase64Array.Length % 4 == 0, "Invariant: Array length must be a multiple of 4.");
            input.CopyTo(offset, completeBase64Array, 0, count);
            for (int i = 1; i <= numPaddingCharsToAdd; i++)
            {
                completeBase64Array[completeBase64Array.Length - i] = '=';
            }

            // Next, fix up '-' -> '+' and '_' -> '/'
            for (int i = 0; i < completeBase64Array.Length; i++)
            {
                char c = completeBase64Array[i];
                if (c == '-')
                {
                    completeBase64Array[i] = '+';
                }
                else if (c == '_')
                {
                    completeBase64Array[i] = '/';
                }
            }

            // Finally, decode.
            // If the caller provided invalid base64 chars, they'll be caught here.
            return Convert.FromBase64CharArray(completeBase64Array, 0, completeBase64Array.Length);
        }

        /// <summary>
        /// Encodes an input using base64url encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <returns>The base64url-encoded form of the input.</returns>
        public static string Base64UrlEncode(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return Base64UrlEncode(input, 0, input.Length);
        }

        /// <summary>
        /// Encodes an input using base64url encoding.
        /// </summary>
        /// <param name="input">The binary input to encode.</param>
        /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
        /// <param name="count">The number of bytes of <paramref name="input"/> to encode.</param>
        /// <returns>The base64url-encoded form of the input.</returns>
        public static string Base64UrlEncode(byte[] input, int offset, int count)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateParameters(input.Length, offset, count);

            // Special-case empty input
            if (count == 0)
            {
                return string.Empty;
            }

            // We're going to use base64url encoding with no padding characters.
            // See RFC 4648, Sec. 5.
            char[] buffer = new char[GetNumBase64CharsRequiredForInput(count)];
            int numBase64Chars = Convert.ToBase64CharArray(input, offset, count, buffer, 0);

            // Fix up '+' -> '-' and '/' -> '_'
            for (int i = 0; i < numBase64Chars; i++)
            {
                char ch = buffer[i];
                if (ch == '+')
                {
                    buffer[i] = '-';
                }
                else if (ch == '/')
                {
                    buffer[i] = '_';
                }
                else if (ch == '=')
                {
                    // We've reached a padding character: truncate the string from this point
                    return new String(buffer, 0, i);
                }
            }

            // If we got this far, the buffer didn't contain any padding chars, so turn
            // it directly into a string.
            return new String(buffer, 0, numBase64Chars);
        }

        private static int GetNumBase64CharsRequiredForInput(int inputLength)
        {
            int numWholeOrPartialInputBlocks = checked(inputLength + 2) / 3;
            return checked(numWholeOrPartialInputBlocks * 4);
        }

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
                    throw new FormatException("TODO: Malformed input.");
            }
        }

        private static void ValidateParameters(int bufferLength, int offset, int count)
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
                throw new ArgumentException("Invalid offset / length.");
            }
        }
    }
}
