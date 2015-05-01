// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Framework.WebEncoders
{
    internal class TestJavaScriptEncoder : IJavaScriptStringEncoder
    {
        public string JavaScriptStringEncode(string value)
        {
            return $"JavaScriptEncode[[{ value }]]";
        }

        public void JavaScriptStringEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            output.Write($"JavaScriptEncode[[{ value.Substring(startIndex, charCount) }]]");
        }

        public void JavaScriptStringEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            output.Write("JavaScriptEncode[[");
            output.Write(value, startIndex, charCount);
            output.Write("]]");
        }
    }
}