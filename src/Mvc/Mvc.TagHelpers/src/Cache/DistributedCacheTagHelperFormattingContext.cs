// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache
{
    /// <summary>
    /// Represents an object containing the information to serialize with <see cref="IDistributedCacheTagHelperFormatter" />.
    /// </summary>
    public class DistributedCacheTagHelperFormattingContext
    {
        /// <summary>
        /// Gets the <see cref="HtmlString"/> instance.
        /// </summary>
        public HtmlString Html { get; set; }
    }
}
