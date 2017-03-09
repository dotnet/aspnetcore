// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ControllersFromServicesWebSite.TagHelpers
{
    [HtmlTargetElement("InServices")]
    public class InServicesTagHelper : TagHelper
    {
        private ValueService _value;

        public InServicesTagHelper(ValueService value)
        {
            _value = value;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.Content.SetContent(_value.Value.ToString());
        }
    }
}
