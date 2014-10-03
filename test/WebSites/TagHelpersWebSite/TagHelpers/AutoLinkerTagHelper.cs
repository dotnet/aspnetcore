// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [TagName("p")]
    [ContentBehavior(ContentBehavior.Modify)]
    public class AutoLinkerTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Find Urls in the content and replace them with their anchor tag equivalent.
            output.Content = Regex.Replace(
                output.Content,
                @"\b(?:https?://|www\.)(\S+)\b",
                "<strong><a target=\"_blank\" href=\"http://$0\">$0</a></strong>");
        }
    }
}