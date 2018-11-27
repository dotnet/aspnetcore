// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [HtmlTargetElement("root")]
    public class RootViewStartTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content.AppendHtml("root-content");
        }
    }
}