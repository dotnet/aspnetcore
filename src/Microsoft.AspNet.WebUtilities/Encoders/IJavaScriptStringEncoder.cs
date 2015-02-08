// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.WebUtilities.Encoders
{
    /// <summary>
    /// Provides services for JavaScript-escaping strings.
    /// </summary>
    public interface IJavaScriptStringEncoder
    {
        /// <summary>
        /// JavaScript-escapes a given input string.
        /// </summary>
        /// <returns>
        /// The JavaScript-escaped value, or null if the input string was null.
        /// </returns>
        string JavaScriptStringEncode(string value);
    }
}
