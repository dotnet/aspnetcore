// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Framework.WebEncoders
{
    internal class TestUrlEncoder : IUrlEncoder
    {
        public string UrlEncode(string value)
        {
            return $"UrlEncode[[{ value }]]";
        }

        public void UrlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            output.Write($"UrlEncode[[{ value.Substring(startIndex, charCount) }]]");
        }

        public void UrlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            output.Write("UrlEncode[[");
            output.Write(value, startIndex, charCount);
            output.Write("]]");
        }
    }
}