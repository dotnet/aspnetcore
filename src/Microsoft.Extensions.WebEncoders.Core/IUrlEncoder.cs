// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Provides services for URL-escaping strings.
    /// </summary>
    public interface IUrlEncoder
    {
        /// <summary>
        /// URL-escapes a character array and writes the result to the supplied
        /// output.
        /// </summary>
        /// <remarks>
        /// The encoded value is appropriately encoded for inclusion in the segment, query, or
        /// fragment portion of a URI.
        /// </remarks>
        void UrlEncode(char[] value, int startIndex, int charCount, TextWriter output);

        /// <summary>
        /// URL-escapes a given input string.
        /// </summary>
        /// <returns>
        /// The URL-escaped value, or null if the input string was null.
        /// </returns>
        /// <remarks>
        /// The return value is appropriately encoded for inclusion in the segment, query, or
        /// fragment portion of a URI.
        /// </remarks>
        string UrlEncode(string value);

        /// <summary>
        /// URL-escapes a string and writes the result to the supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is appropriately encoded for inclusion in the segment, query, or
        /// fragment portion of a URI.
        /// </remarks>
        void UrlEncode(string value, int startIndex, int charCount, TextWriter output);
    }
}
