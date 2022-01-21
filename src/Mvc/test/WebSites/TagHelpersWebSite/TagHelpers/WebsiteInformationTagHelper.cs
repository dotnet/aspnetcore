// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using TagHelpersWebSite.Models;

namespace TagHelpersWebSite.TagHelpers;

public class WebsiteInformationTagHelper : TagHelper
{
    public WebsiteContext Info { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "section";
        output.PostContent.AppendHtml(string.Format(
            CultureInfo.InvariantCulture,
            "<p><strong>Version:</strong> {0}</p>" + Environment.NewLine +
            "<p><strong>Copyright Year:</strong> {1}</p>" + Environment.NewLine +
            "<p><strong>Approved:</strong> {2}</p>" + Environment.NewLine +
            "<p><strong>Number of tags to show:</strong> {3}</p>" + Environment.NewLine,
            Info.Version,
            Info.CopyrightYear,
            Info.Approved,
            Info.TagsToShow));
        output.TagMode = TagMode.StartTagAndEndTag;
    }
}
