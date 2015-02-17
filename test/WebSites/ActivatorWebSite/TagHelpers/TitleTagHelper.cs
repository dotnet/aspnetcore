// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlElementName("body")]
    public class TitleTagHelper : TagHelper
    {
        [Activate]
        public IHtmlHelper HtmlHelper { get; set; }

        [Activate]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var builder = new TagBuilder("h2", HtmlHelper.HtmlEncoder);
            var title = ViewContext.ViewBag.Title;
            builder.InnerHtml = HtmlHelper.Encode(title);
            output.PreContent = builder.ToString();
        }
    }
}