// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelperSample.Web
{
    [HtmlTargetElement("iecondition")]
    public class ConditionalCommentTagHelper : TagHelper
    {
        public CommentMode Mode { get; set; }

        public string Condition { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;

            var modeModifier = string.Empty;

            if (Mode == CommentMode.DownlevelHidden)
            {
                modeModifier = "--";
            }

            output.PreContent.AppendHtml("<!");
            output.PreContent.AppendHtml(modeModifier);
            output.PreContent.AppendHtml("[if ");
            output.PreContent.AppendHtml(Condition);
            output.PreContent.AppendHtml("]>");

            output.PostContent.AppendHtml("<![endif]");
            output.PostContent.AppendHtml(modeModifier);
            output.PostContent.AppendHtml(">");
        }
    }
}
