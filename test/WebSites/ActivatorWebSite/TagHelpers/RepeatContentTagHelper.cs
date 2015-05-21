// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [TargetElement("div")]
    public class RepeatContentTagHelper : TagHelper
    {
        public RepeatContentTagHelper(IHtmlHelper htmlHelper)
        {
            HtmlHelper = htmlHelper;
        }

        public IHtmlHelper HtmlHelper { get; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public int RepeatContent { get; set; }

        public ModelExpression Expression { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            (HtmlHelper as ICanHasViewContext)?.Contextualize(ViewContext);

            var content = await context.GetChildContentAsync();
            var repeatContent = HtmlHelper.Encode(Expression.Model.ToString());

            if (string.IsNullOrEmpty(repeatContent))
            {
                repeatContent = content.GetContent();
            }

            for (int i = 0; i < RepeatContent; i++)
            {
                output.Content.Append(repeatContent);
            }
        }

    }
}