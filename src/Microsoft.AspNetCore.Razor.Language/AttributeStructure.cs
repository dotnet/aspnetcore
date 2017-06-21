// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    // This is the design time equivalent of Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.
    // They should be kept in sync.
    public enum AttributeStructure
    {
        DoubleQuotes,
        SingleQuotes,
        NoQuotes,
        Minimized,
    }
}
