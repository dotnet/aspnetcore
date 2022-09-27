// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers;

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
