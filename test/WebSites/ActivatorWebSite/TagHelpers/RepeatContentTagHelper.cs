// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlElementName("div")]
    [ContentBehavior(ContentBehavior.Modify)]
    public class RepeatContentTagHelper : TagHelper
    {
        public int RepeatContent { get; set; }

        public ModelExpression Expression { get; set; }

        [Activate]
        public IHtmlHelper HtmlHelper { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var repeatContent = HtmlHelper.Encode(Expression.Metadata.Model.ToString());

            if (string.IsNullOrEmpty(repeatContent))
            {
                repeatContent = output.Content;
                output.Content = string.Empty;
            }

            for (int i = 0; i < RepeatContent; i++)
            {
                output.Content += repeatContent;
            }
        }

    }
}