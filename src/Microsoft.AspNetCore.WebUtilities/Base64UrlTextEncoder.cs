// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    public static class Base64UrlTextEncoder
    {
        /// <summary>
        /// Encodes supplied data into Base64 and replaces any URL encodable characters into non-URL encodable
        /// characters.
        /// </summary>
        /// <param name="data">Data to be encoded.</param>
        /// <returns>Base64 encoded string modified with non-URL encodable characters</returns>
        public static string Encode(byte[] data)
        {
            var encodedValue = Convert.ToBase64String(data);
            return EncodeInternal(encodedValue);
        }

        /// <summary>
        /// Decodes supplied string by replacing the non-URL encodable characters with URL encodable characters and
        /// then decodes the Base64 string.
        /// </summary>
        /// <param name="text">The string to be decoded.</param>
        /// <returns>The decoded data.</returns>
        public static byte[] Decode(string text)
        {
            return Convert.FromBase64String(DecodeToBase64String(text));
        }

        // To enable unit testing
        internal static string EncodeInternal(string base64EncodedString)
        {
            var length = base64EncodedString.Length;
            while (length > 0 && base64EncodedString[length - 1] == '=')
            {
                length--;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            var inplaceStringBuilder = new InplaceStringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                if (base64EncodedString[i] == '+')
                {
                    inplaceStringBuilder.Append('-');
                }
                else if (base64EncodedString[i] == '/')
                {
                    inplaceStringBuilder.Append('_');
                }
                else
                {
                    inplaceStringBuilder.Append(base64EncodedString[i]);
                }
            }

            return inplaceStringBuilder.ToString();
        }

        // To enable unit testing
        internal static string DecodeToBase64String(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var padLength = 3 - ((text.Length + 3) % 4);
            var inplaceStringBuilder = new InplaceStringBuilder(capacity: text.Length + padLength);

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '-')
                {
                    inplaceStringBuilder.Append('+');
                }
                else if (text[i] == '_')
                {
                    inplaceStringBuilder.Append('/');
                }
                else
                {
                    inplaceStringBuilder.Append(text[i]);
                }
            }

            for (var i = 0; i < padLength; i++)
            {
                inplaceStringBuilder.Append('=');
            }

            return inplaceStringBuilder.ToString();
        }
    }
}
