// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AngleSharp.Dom;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public static class IHtmlDocumentExtensions
{
    public static IElement RequiredQuerySelector(this IHtmlDocument document, string selector)
    {
        var element = document.QuerySelector(selector);
        if (element == null)
        {
            throw new ArgumentException($"Document does not contain element that matches the selector {selector}: " + Environment.NewLine + document.DocumentElement.OuterHtml);
        }

        return element;
    }

    public static string RetrieveAntiforgeryToken(this IHtmlDocument htmlDocument)
    {
        var hiddenInputs = htmlDocument.QuerySelectorAll("form input[type=hidden]");
        foreach (var input in hiddenInputs)
        {
            if (!input.HasAttribute("name"))
            {
                continue;
            }

            var name = input.GetAttribute("name");
            if (name == "__RequestVerificationToken" || name == "HtmlEncode[[__RequestVerificationToken]]")
            {
                return input.GetAttribute("value");
            }
        }

        throw new Exception($"Antiforgery token could not be located in {htmlDocument.Source.Text}.");
    }
}
