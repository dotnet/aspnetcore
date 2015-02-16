// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlElementName("body")]
    public class FooterTagHelper : TagHelper
    {
        [Activate]
        public IHtmlHelper HtmlHelper { get; set; }

        [Activate]
        public ViewDataDictionary ViewData { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.PostContent = $"<footer>{HtmlHelper.Encode(ViewData["footer"])}</footer>";
        }
    }
}