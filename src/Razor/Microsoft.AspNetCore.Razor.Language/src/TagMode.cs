// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// The mode in which an element should render.
    /// </summary>
    public enum TagMode
    {
        /// <summary>
        /// Include both start and end tags.
        /// </summary>
        StartTagAndEndTag,

        /// <summary>
        /// A self-closed tag.
        /// </summary>
        SelfClosing,

        /// <summary>
        /// Only a start tag.
        /// </summary>
        StartTagOnly
    }
}
