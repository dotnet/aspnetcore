// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.WebUtilities.Encoders
{
    /// <summary>
    /// Provides services for URL-escaping strings.
    /// </summary>
    public interface IUrlEncoder
    {
        /// <summary>
        /// URL-escapes a given input string.
        /// </summary>
        /// <returns>
        /// The URL-escaped value, or null if the input string was null.
        /// </returns>
        /// <remarks>
        /// The return value is safe for use in the segment, query, or
        /// fragment portion of a URI.
        /// </remarks>
        string UrlEncode(string value);
    }
}
