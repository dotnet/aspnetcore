// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// A <see cref="TagHelperComponentTagHelper"/> targeting the &lt;body&gt; HTML element.
    /// </summary>
    [HtmlTargetElement("body")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BodyTagHelper : TagHelperComponentTagHelper
    {
        /// <summary>
        /// Creates a new <see cref="BodyTagHelper"/>.
        /// </summary>
        /// <param name="manager">The <see cref="ITagHelperComponentManager"/> which contains the collection
        /// of <see cref="ITagHelperComponent"/>s.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public BodyTagHelper(ITagHelperComponentManager manager, ILoggerFactory loggerFactory)
            : base(manager, loggerFactory)
        {
        }
    }
}
