// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using System.Threading.Tasks;

namespace ActivatorWebSite.TagHelpers
{
    [HtmlElementName("span")]
    public class HiddenTagHelper : TagHelper
    {
        public string Name { get; set; }

        [Activate]
        public IHtmlHelper HtmlHelper { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await context.GetChildContentAsync();

            output.Content = HtmlHelper.Hidden(Name, content).ToString();
        }
    }
}