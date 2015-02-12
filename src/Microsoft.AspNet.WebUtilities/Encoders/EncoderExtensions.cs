// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.WebUtilities.Encoders
{
    /// <summary>
    /// Helpful extension methods for the encoder classes.
    /// </summary>
    public static class EncoderExtensions
    {
        /// <summary>
        /// HTML-encodes a string and writes the result to the supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is also safe for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        public static void HtmlEncode([NotNull] this IHtmlEncoder htmlEncoder, string value, [NotNull] TextWriter output)
        {
            if (!String.IsNullOrEmpty(value))
            {
                htmlEncoder.HtmlEncode(value, 0, value.Length, output);
            }
        }

        /// <summary>
        /// JavaScript-escapes a string and writes the result to the supplied output.
        /// </summary>
        public static void JavaScriptStringEncode([NotNull] this IJavaScriptStringEncoder javaScriptStringEncoder, string value, [NotNull] TextWriter output)
        {
            if (!String.IsNullOrEmpty(value))
            {
                javaScriptStringEncoder.JavaScriptStringEncode(value, 0, value.Length, output);
            }
        }

        /// <summary>
        /// URL-encodes a string and writes the result to the supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is safe for use in the segment, query, or
        /// fragment portion of a URI.
        /// </remarks>
        public static void UrlEncode([NotNull] this IUrlEncoder urlEncoder, string value, [NotNull] TextWriter output)
        {
            if (!String.IsNullOrEmpty(value))
            {
                urlEncoder.UrlEncode(value, 0, value.Length, output);
            }
        }
    }
}
