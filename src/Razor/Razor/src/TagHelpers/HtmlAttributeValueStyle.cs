// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Determines how an HTML attribute value appears in markup.
/// </summary>
public enum HtmlAttributeValueStyle
{
    /// <summary>
    /// An attribute value that appears in double quotes.
    /// </summary>
    DoubleQuotes,

    /// <summary>
    /// An attribute value that appears in single quotes.
    /// </summary>
    SingleQuotes,

    /// <summary>
    /// An attribute value that appears without quotes.
    /// </summary>
    NoQuotes,

    /// <summary>
    /// A minimized attribute value.
    /// </summary>
    Minimized,
}
