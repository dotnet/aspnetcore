// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace TagHelperSample.Web
{
    [TargetElement("iecondition")]
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

            output.PreContent.Append("<!");
            output.PreContent.Append(modeModifier);
            output.PreContent.Append("[if ");
            output.PreContent.Append(Condition);
            output.PreContent.Append("]>");

            output.PostContent.Append("<![endif]");
            output.PostContent.Append(modeModifier);
            output.PostContent.Append(">");
        }
    }
}
