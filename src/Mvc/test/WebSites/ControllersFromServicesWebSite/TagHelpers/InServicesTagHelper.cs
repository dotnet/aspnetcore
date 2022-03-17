// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ControllersFromServicesWebSite.TagHelpers;

[HtmlTargetElement("InServices")]
public class InServicesTagHelper : TagHelper
{
    private readonly ValueService _value;

    public InServicesTagHelper(ValueService value)
    {
        _value = value;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        output.Content.SetContent(_value.Value.ToString(CultureInfo.InvariantCulture));
    }
}
