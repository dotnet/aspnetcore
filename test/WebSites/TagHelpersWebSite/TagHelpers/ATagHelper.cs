// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    public class ATagHelper : TagHelper
    {
        public ATagHelper(IUrlHelper urlHelper)
        {
            UrlHelper = urlHelper;
        }

        [HtmlAttributeNotBound]
        public IUrlHelper UrlHelper { get; }

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

                output.Attributes["href"] = UrlHelper.Action(Action, Controller, methodParameters);

                output.PreContent.SetContent("My ");
            }
        }
    }
}