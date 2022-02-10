// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers;

[HtmlTargetElement(Attributes = nameof(Surround))]
public class SurroundTagHelper : TagHelper
{
    public override int Order
    {
        get
        {
            // Run first
            return int.MinValue;
        }
    }

    public string Surround { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var surroundingTagName = Surround.ToLowerInvariant();

        output.PreElement.AppendHtml($"<{surroundingTagName}>");
        output.PostElement.AppendHtml($"</{surroundingTagName}>");
    }
}
