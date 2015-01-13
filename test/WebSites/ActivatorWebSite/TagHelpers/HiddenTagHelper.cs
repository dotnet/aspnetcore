// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlElementName("span")]
    [ContentBehavior(ContentBehavior.Modify)]
    public class HiddenTagHelper : TagHelper
    {
        public string Name { get; set; }

        [Activate]
        public IHtmlHelper HtmlHelper { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content = HtmlHelper.Hidden(Name, output.Content).ToString();
        }
    }
}