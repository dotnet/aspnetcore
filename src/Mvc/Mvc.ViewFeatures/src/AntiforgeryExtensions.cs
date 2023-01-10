// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Static class that adds extension methods to <see cref="IAntiforgery"/>. This class cannot be inherited.
/// </summary>
public static class AntiforgeryExtensions
{
    /// <summary>
    /// Generates an &lt;input type="hidden"&gt; element for an antiforgery token.
    /// </summary>
    /// <param name="antiforgery">The <see cref="IAntiforgery"/> instance.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <returns>
    /// A <see cref="IHtmlContent"/> containing an &lt;input type="hidden"&gt; element. This element should be put
    /// inside a &lt;form&gt;.
    /// </returns>
    /// <remarks>
    /// This method has a side effect:
    /// A response cookie is set if there is no valid cookie associated with the request.
    /// </remarks>
    public static IHtmlContent GetHtml(this IAntiforgery antiforgery, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(antiforgery);
        ArgumentNullException.ThrowIfNull(httpContext);

        var tokenSet = antiforgery.GetAndStoreTokens(httpContext);
        return new InputContent(tokenSet);
    }

    private sealed class InputContent : IHtmlContent
    {
        private readonly string _fieldName;
        private readonly string _requestToken;

        public InputContent(AntiforgeryTokenSet tokenSet)
        {
            _fieldName = tokenSet.FormFieldName;
            _requestToken = tokenSet.RequestToken!;
        }

        // Though _requestToken normally contains only US-ASCII letters, numbers, '-', and '_', must assume the
        // IAntiforgeryTokenSerializer implementation has been overridden. Similarly, users may choose a
        // _fieldName containing almost any character.
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write("<input name=\"");
            encoder.Encode(writer, _fieldName);
            writer.Write("\" type=\"hidden\" value=\"");
            encoder.Encode(writer, _requestToken);
            writer.Write("\" />");
        }
    }
}
