// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
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
}
