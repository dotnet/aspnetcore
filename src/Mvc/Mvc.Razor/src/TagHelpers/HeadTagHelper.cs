// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

/// <summary>
/// A <see cref="TagHelperComponentTagHelper"/> targeting the &lt;head&gt; HTML element.
/// </summary>
[HtmlTargetElement("head")]
[EditorBrowsable(EditorBrowsableState.Never)]
public class HeadTagHelper : TagHelperComponentTagHelper
{
    /// <summary>
    /// Creates a new <see cref="HeadTagHelper"/>.
    /// </summary>
    /// <param name="manager">The <see cref="ITagHelperComponentManager"/> which contains the collection
    /// of <see cref="ITagHelperComponent"/>s.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public HeadTagHelper(ITagHelperComponentManager manager, ILoggerFactory loggerFactory)
        : base(manager, loggerFactory)
    {
    }
}
