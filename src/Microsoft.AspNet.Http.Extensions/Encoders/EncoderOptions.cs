// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.WebUtilities.Encoders
{
    /// <summary>
    /// Specifies options common to all three encoders (HtmlEncode, JavaScriptStringEncode, UrlEncode).
    /// </summary>
    public sealed class EncoderOptions
    {
        /// <summary>
        /// Specifies code point tables which do not require escaping by the encoders.
        /// </summary>
        /// <remarks>
        /// By default, only Basic Latin is allowed.
        /// </remarks>
        public ICodePointFilter[] CodePointFilters { get; set; } = new[] { Microsoft.AspNet.WebUtilities.Encoders.CodePointFilters.BasicLatin };
    }
}
