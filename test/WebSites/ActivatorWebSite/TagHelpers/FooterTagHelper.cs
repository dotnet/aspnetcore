// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlTargetElement("body")]
    public class FooterTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.PostContent
                .AppendHtml("<footer>")
                .Append((string)ViewContext.ViewData["footer"])
                .AppendHtml("</footer>");
        }
    }
}