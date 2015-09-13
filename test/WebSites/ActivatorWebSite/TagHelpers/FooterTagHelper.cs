// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.WebEncoders;

namespace ActivatorWebSite.TagHelpers
{
    [TargetElement("body")]
    public class FooterTagHelper : TagHelper
    {
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.PostContent
                .AppendEncoded("<footer>")
                .Append((string)ViewContext.ViewData["footer"])
                .AppendEncoded("</footer>");
        }
    }
}