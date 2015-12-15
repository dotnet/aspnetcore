// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Html;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <inheritdoc />
    public class HtmlString : HtmlEncodedString
    {
        /// <summary>
        /// Returns an <see cref="HtmlString"/> with empty content.
        /// </summary>
        public static readonly HtmlString Empty = new HtmlString(string.Empty);

        /// <summary>
        /// Creates a new instance of <see cref="HtmlString"/>.
        /// </summary>
        /// <param name="input">The HTML encoded value.</param>
        public HtmlString(string input)
            : base(input)
        {
        }
    }
}
