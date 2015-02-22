// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.WebEncoders
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
        /// If this property is set to a null array, then by default only the 'Basic Latin'
        /// code point filter is active.
        /// </remarks>
        public ICodePointFilter[] CodePointFilters { get; set; }
    }
}
