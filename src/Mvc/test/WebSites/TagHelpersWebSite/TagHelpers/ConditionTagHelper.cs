// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers;

[HtmlTargetElement("div")]
[HtmlTargetElement("style")]
[HtmlTargetElement("p")]
public class ConditionTagHelper : TagHelper
{
    public bool? Condition { get; set; }

    public override int Order
    {
        get
        {
            // Run after other tag helpers targeting the same element. Other tag helpers have Order <= 0.
            return 1000;
        }
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // If a condition is set and evaluates to false, don't render the tag.
        if (Condition.HasValue && !Condition.Value)
        {
            output.SuppressOutput();
        }
    }
}
