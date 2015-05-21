// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace TagHelpersWebSite.TagHelpers
{
    [TargetElement("*")]
    public class PrettyTagHelper : TagHelper
    {
        private static readonly Dictionary<string, string> PrettyTagStyles =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "a", "background-color: gray;color: white;border-radius: 3px;"
                    + "border: 1px solid black;padding: 3px;font-family: cursive;" },
                { "strong", "font-size: 1.25em;text-decoration: underline;" },
                { "h1", "font-family: cursive;" },
                { "h3", "font-family: cursive;" }
            };

        public bool? MakePretty { get; set; }

        public string Style { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Need to check if output.TagName == null in-case the ConditionTagHelper calls into SuppressOutput and
            // therefore sets the TagName to null.
            if (MakePretty.HasValue && !MakePretty.Value ||
                output.TagName == null)
            {
                return;
            }

            string prettyStyle;

            if (PrettyTagStyles.TryGetValue(output.TagName, out prettyStyle))
            {
                var style = Style ?? string.Empty;
                if (!string.IsNullOrEmpty(style))
                {
                    style += ";";
                }

                output.Attributes["style"] = style + prettyStyle;
            }
        }
    }
}