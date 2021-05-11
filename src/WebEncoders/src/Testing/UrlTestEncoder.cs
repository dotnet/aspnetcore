// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.Extensions.WebEncoders.Testing
{
    /// <summary>
    /// <see cref="UrlEncoder"/> used for unit testing. This encoder does not perform any encoding and should not be used in application code.
    /// </summary>
    public class UrlTestEncoder : UrlEncoder
    {
        /// <inheritdoc />
        public override int MaxOutputCharactersPerInputCharacter
        {
            get { return 1; }
        }

        /// <inheritdoc />
        public override string Encode(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                return string.Empty;
            }

            return $"UrlEncode[[{value}]]";
        }

        /// <inheritdoc />
        public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (characterCount == 0)
            {
                return;
            }

            output.Write("UrlEncode[[");
            output.Write(value, startIndex, characterCount);
            output.Write("]]");
        }

        /// <inheritdoc />
        public override void Encode(TextWriter output, string value, int startIndex, int characterCount)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (characterCount == 0)
            {
                return;
            }

            output.Write("UrlEncode[[");
            output.Write(value.Substring(startIndex, characterCount));
            output.Write("]]");
        }

        /// <inheritdoc />
        public override bool WillEncode(int unicodeScalar)
        {
            return false;
        }

        /// <inheritdoc />
        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            return -1;
        }

        /// <inheritdoc />
        public override unsafe bool TryEncodeUnicodeScalar(
            int unicodeScalar,
            char* buffer,
            int bufferLength,
            out int numberOfCharactersWritten)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            numberOfCharactersWritten = 0;
            return false;
        }
    }
}
