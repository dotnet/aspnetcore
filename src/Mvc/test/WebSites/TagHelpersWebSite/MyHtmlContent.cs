// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TagHelpersWebSite;

public class MyHtmlContent : IHtmlContent
{
    private IHtmlHelper Html { get; }

    public MyHtmlContent(IHtmlHelper html)
    {
        Html = html;
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
#pragma warning disable MVC1000 // Use of IHtmlHelper.{0} should be avoided.
        Html.Partial("_Test").WriteTo(writer, encoder);
#pragma warning restore MVC1000 // Use of IHtmlHelper.{0} should be avoided.
    }
}
