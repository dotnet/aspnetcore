// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [TagName("div", "style", "p")]
    [ContentBehavior(ContentBehavior.Modify)]
    public class ConditionTagHelper : TagHelper
    {
        public bool? Condition { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // If a condition is set and evaluates to false, don't render the tag.
            if (Condition.HasValue && !Condition.Value)
            {
                output.TagName = null;
                output.Content = null;
            }
        }
    }
}