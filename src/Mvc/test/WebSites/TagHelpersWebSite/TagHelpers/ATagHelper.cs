// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    public class ATagHelper : TagHelper
    {
        public ATagHelper(IUrlHelperFactory urlHelperFactory)
        {
            UrlHelperFactory = urlHelperFactory;
        }

        [HtmlAttributeNotBound]
        public IUrlHelperFactory UrlHelperFactory { get; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public string Controller { get; set; }

        public string Action { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (Controller != null && Action != null)
            {
                var methodParameters = output.Attributes.ToDictionary(attribute => attribute.Name,
                                                                      attribute => attribute.Value);

                // We remove all attributes from the resulting HTML element because they're supposed to
                // be parameters to our final href value.
                output.Attributes.Clear();

                var urlHelper = UrlHelperFactory.GetUrlHelper(ViewContext);
                output.Attributes.SetAttribute("href", urlHelper.Action(Action, Controller, methodParameters));

                output.PreContent.SetContent("My ");
            }
        }
    }
}