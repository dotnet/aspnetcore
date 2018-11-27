// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [HtmlTargetElement("pre")]
    public class NullEncoderTagHelper : TagHelper
    {
        public override int Order => 3;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var nullContent = await output.GetChildContentAsync(NullHtmlEncoder.Default);

            // Note this is very unsafe. Should always post-process content that may not be fully HTML encoded before
            // writing it into a response. Here for example, could pass SetContent() a string and that would be
            // HTML encoded later.
            output.PostContent
                .SetHtmlContent("<br />Null encoder: ")
                .AppendHtml(nullContent);
        }
    }
}
