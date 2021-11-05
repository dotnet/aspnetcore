// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

// This is the design time equivalent of Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.
// They should be kept in sync.
public enum AttributeStructure
{
    DoubleQuotes,
    SingleQuotes,
    NoQuotes,
    Minimized,
}
