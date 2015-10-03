// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Encoder used for unit testing.
    /// </summary>
    internal sealed class CommonTestEncoder : IHtmlEncoder, IJavaScriptStringEncoder, IUrlEncoder
    {
        /// <summary>
        /// Returns "HtmlEncode[[value]]".
        /// </summary>
        public string HtmlEncode(string value)
        {
            return EncodeCore(value);
        }

        /// <summary>
        /// Writes "HtmlEncode[[value]]".
        /// </summary>
        public void HtmlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        /// <summary>
        /// Writes "HtmlEncode[[value]]".
        /// </summary>
        public void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        /// <summary>
        /// Returns "JavaScriptStringEncode[[value]]".
        /// </summary>
        public string JavaScriptStringEncode(string value)
        {
            return EncodeCore(value);
        }

        /// <summary>
        /// Writes "JavaScriptStringEncode[[value]]".
        /// </summary>
        public void JavaScriptStringEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        /// <summary>
        /// Writes "JavaScriptStringEncode[[value]]".
        /// </summary>
        public void JavaScriptStringEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        /// <summary>
        /// Returns "UrlEncode[[value]]".
        /// </summary>
        public string UrlEncode(string value)
        {
            return EncodeCore(value);
        }

        /// <summary>
        /// Writes "UrlEncode[[value]]".
        /// </summary>
        public void UrlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        /// <summary>
        /// Writes "UrlEncode[[value]]".
        /// </summary>
        public void UrlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        private static string EncodeCore(string value, [CallerMemberName] string encodeType = null)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0}[[{1}]]", encodeType, value);
        }

        private static void EncodeCore(string value, int startIndex, int charCount, TextWriter output, [CallerMemberName] string encodeType = null)
        {
            output.Write(EncodeCore(value.Substring(startIndex, charCount), encodeType));
        }

        private static void EncodeCore(char[] value, int startIndex, int charCount, TextWriter output, [CallerMemberName] string encodeType = null)
        {
            output.Write(EncodeCore(new string(value, startIndex, charCount), encodeType));
        }
    }
}