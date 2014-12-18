// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [HtmlElementName("p")]
    public class AutoLinkerTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var childContent = await context.GetChildContentAsync();

            // Find Urls in the content and replace them with their anchor tag equivalent.
            output.Content = Regex.Replace(
                childContent,
                @"\b(?:https?://|www\.)(\S+)\b",
                "<strong><a target=\"_blank\" href=\"http://$0\">$0</a></strong>");
        }
    }
}