// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers;

[HtmlTargetElement(Attributes = "bold")]
public class BoldTagHelper : TagHelper
{
    public override int Order
    {
        get
        {
            return int.MinValue;
        }
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("bold");
        output.PreContent.AppendHtml("<b>");
        output.PostContent.AppendHtml("</b>");
    }
}
