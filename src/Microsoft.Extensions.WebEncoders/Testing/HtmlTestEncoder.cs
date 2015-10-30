// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.Extensions.WebEncoders.Testing
{
    /// <summary>
    /// Encoder used for unit testing.
    /// </summary>
    public sealed class HtmlTestEncoder : HtmlEncoder
    {
        public override int MaxOutputCharactersPerInputCharacter
        {
            get { return 1; }
        }

        public override string Encode(string value)
        {
            return $"HtmlEncode[[{value}]]";
        }

        public override void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
        {
            output.Write("HtmlEncode[[");
            output.Write(value, startIndex, characterCount);
            output.Write("]]");
        }

        public override void Encode(TextWriter output, string value, int startIndex, int characterCount)
        {
            output.Write("HtmlEncode[[");
            output.Write(value.Substring(startIndex, characterCount));
            output.Write("]]");
        }

        public override bool WillEncode(int unicodeScalar)
        {
            return false;
        }

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            return -1;
        }

        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            numberOfCharactersWritten = 0;
            return false;
        }
    }
}