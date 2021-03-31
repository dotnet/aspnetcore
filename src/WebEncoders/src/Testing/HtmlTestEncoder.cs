// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.Extensions.WebEncoders.Testing
{
    /// <summary>
    /// <see cref="HtmlEncoder"/> used for unit testing. This encoder does not perform any encoding and should not be used in application code.
    /// </summary>
    public sealed class HtmlTestEncoder : HtmlEncoder
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

            return $"HtmlEncode[[{value}]]";
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

            output.Write("HtmlEncode[[");
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

            output.Write("HtmlEncode[[");
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
