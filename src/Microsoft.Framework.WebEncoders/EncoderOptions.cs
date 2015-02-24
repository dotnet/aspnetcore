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
        /// Specifies which code points are allowed to be represented unescaped by the encoders.
        /// </summary>
        /// <remarks>
        /// If this property is null, then the encoders will use their default allow lists.
        /// </remarks>
        public ICodePointFilter CodePointFilter { get; set; }
    }
}
