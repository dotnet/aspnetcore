// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.WebEncoders
{
    /// <summary>
    /// Provides services for JavaScript-escaping strings.
    /// </summary>
    public interface IJavaScriptStringEncoder
    {
        /// <summary>
        /// JavaScript-escapes a character array and writes the result to the
        /// supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is appropriately encoded for inclusion inside a quoted JSON string.
        /// </remarks>
        void JavaScriptStringEncode(char[] value, int startIndex, int charCount, TextWriter output);

        /// <summary>
        /// JavaScript-escapes a given input string.
        /// </summary>
        /// <returns>
        /// The JavaScript-escaped value, or null if the input string was null.
        /// The encoded value is appropriately encoded for inclusion inside a quoted JSON string.
        /// </returns>
        string JavaScriptStringEncode(string value);

        /// <summary>
        /// JavaScript-escapes a given input string and writes the
        /// result to the supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is appropriately encoded for inclusion inside a quoted JSON string.
        /// </remarks>
        void JavaScriptStringEncode(string value, int startIndex, int charCount, TextWriter output);
    }
}
