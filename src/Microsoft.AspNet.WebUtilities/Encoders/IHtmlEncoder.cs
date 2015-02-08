// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.WebUtilities.Encoders
{
    /// <summary>
    /// Provides services for HTML-encoding input.
    /// </summary>
    public interface IHtmlEncoder
    {
        /// <summary>
        /// HTML-encodes a given input string.
        /// </summary>
        /// <returns>
        /// The HTML-encoded value, or null if the input string was null.
        /// </returns>
        /// <remarks>
        /// The return value is also safe for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        string HtmlEncode(string value);
    }
}
