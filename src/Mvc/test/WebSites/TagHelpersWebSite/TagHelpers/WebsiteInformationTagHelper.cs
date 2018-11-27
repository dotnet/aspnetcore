// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;
using TagHelpersWebSite.Models;

namespace TagHelpersWebSite.TagHelpers
{
    public class WebsiteInformationTagHelper : TagHelper
    {
        public WebsiteContext Info { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "section";
            output.PostContent.AppendHtml(string.Format(
                "<p><strong>Version:</strong> {0}</p>" + Environment.NewLine +
                "<p><strong>Copyright Year:</strong> {1}</p>" + Environment.NewLine +
                "<p><strong>Approved:</strong> {2}</p>" + Environment.NewLine +
                "<p><strong>Number of tags to show:</strong> {3}</p>" + Environment.NewLine,
                Info.Version.ToString(),
                Info.CopyrightYear.ToString(),
                Info.Approved.ToString(),
                Info.TagsToShow.ToString()));
            output.TagMode = TagMode.StartTagAndEndTag;
        }
    }
}