// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers.Test
{
    public class NullHtmlEncoder : IHtmlEncoder
    {
        public string HtmlEncode(string value)
        {
            return value;
        }

        public void HtmlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            output.Write(value.Substring(startIndex, charCount));
        }

        public void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            output.Write(value, startIndex, charCount);
        }
    }
}