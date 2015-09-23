// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Mvc.ViewFeatures.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.WebEncoders;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlTargetElement("span")]
    public class HiddenTagHelper : TagHelper
    {
        public HiddenTagHelper(IHtmlHelper htmlHelper, IHtmlEncoder htmlEncoder)
        {
            HtmlHelper = htmlHelper;
            HtmlEncoder = htmlEncoder;
        }

        public IHtmlHelper HtmlHelper { get; }

        public IHtmlEncoder HtmlEncoder { get; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public string Name { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            (HtmlHelper as ICanHasViewContext)?.Contextualize(ViewContext);

            var content = await context.GetChildContentAsync();
            output.Content.SetContent(HtmlHelper.Hidden(Name, content.GetContent(HtmlEncoder)));
        }
    }
}