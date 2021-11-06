// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Razor.TagHelpers;

public class TagHelperAttribute : IHtmlContentContainer
{
    public TagHelperAttribute(string name)
    {
    }

    public TagHelperAttribute(string name, object value)
    {
    }

    public TagHelperAttribute(string name, object value, HtmlAttributeValueStyle valueStyle)
    {
    }

    public string Name { get; }

    public object Value { get; }

    public HtmlAttributeValueStyle ValueStyle { get; }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
    }

    public void CopyTo(IHtmlContentBuilder destination)
    {
    }

    public void MoveTo(IHtmlContentBuilder destination)
    {
    }
}
