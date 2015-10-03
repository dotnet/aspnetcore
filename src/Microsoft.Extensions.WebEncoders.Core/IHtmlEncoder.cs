// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Provides services for HTML-encoding input.
    /// </summary>
    public interface IHtmlEncoder
    {
        /// <summary>
        /// HTML-encodes a character array and writes the result to the supplied
        /// output.
        /// </summary>
        /// <remarks>
        /// The encoded value is also appropriately encoded for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output);

        /// <summary>
        /// HTML-encodes a given input string.
        /// </summary>
        /// <returns>
        /// The HTML-encoded value, or null if the input string was null.
        /// </returns>
        /// <remarks>
        /// The return value is also appropriately encoded for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        string HtmlEncode(string value);

        /// <summary>
        /// HTML-encodes a given input string and writes the result to the
        /// supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is also appropriately encoded for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        void HtmlEncode(string value, int startIndex, int charCount, TextWriter output);
    }
}
