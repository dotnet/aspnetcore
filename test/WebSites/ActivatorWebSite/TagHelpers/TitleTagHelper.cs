// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [TargetElement("body")]
    public class TitleTagHelper : TagHelper
    {
        public TitleTagHelper(IHtmlHelper htmlHelper)
        {
            HtmlHelper = htmlHelper;
        }

        public IHtmlHelper HtmlHelper { get; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            (HtmlHelper as ICanHasViewContext)?.Contextualize(ViewContext);

            var builder = new TagBuilder("h2", HtmlHelper.HtmlEncoder);
            var title = ViewContext.ViewBag.Title;
            builder.InnerHtml = HtmlHelper.Encode(title);
            output.PreContent.SetContent(builder.ToString());
        }
    }
}