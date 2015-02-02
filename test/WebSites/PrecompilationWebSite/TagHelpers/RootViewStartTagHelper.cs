// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace PrecompilationWebSite.TagHelpers
{
    [HtmlElementName("root")]
    public class RootViewStartTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes["data-root"] = "true";
        }
    }
}