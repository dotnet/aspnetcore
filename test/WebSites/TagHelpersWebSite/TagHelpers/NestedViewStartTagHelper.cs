// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [TagName("nested")]
    [ContentBehavior(ContentBehavior.Modify)]
    public class NestedViewStartTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content = "nested-content";
        }
    }
}