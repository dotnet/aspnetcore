// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Framework.WebEncoders
{
    internal class TestHtmlEncoder : IHtmlEncoder
    {
        public string HtmlEncode(string value)
        {
            return $"HtmlEncode[[{ value }]]";
        }

        public void HtmlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            output.Write($"HtmlEncode[[{ value.Substring(startIndex, charCount) }]]");
        }

        public void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            output.Write("HtmlEncode[[");
            output.Write(value, startIndex, charCount);
            output.Write("]]");
        }
    }
}