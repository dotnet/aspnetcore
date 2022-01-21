// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers;

[HtmlTargetElement(Attributes = "prefix-*")]
public class DictionaryPrefixTestTagHelper : TagHelper
{
    [HtmlAttributeName(DictionaryAttributePrefix = "prefix-")]
    public IDictionary<string, ModelExpression> PrefixValues { get; set; } = new Dictionary<string, ModelExpression>();

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var ulTag = new TagBuilder("ul");

        foreach (var item in PrefixValues)
        {
            var liTag = new TagBuilder("li");

            liTag.InnerHtml.Append(item.Value.Name);

            ulTag.InnerHtml.AppendHtml(liTag);
        }

        output.Content.SetHtmlContent(ulTag);
    }
}
