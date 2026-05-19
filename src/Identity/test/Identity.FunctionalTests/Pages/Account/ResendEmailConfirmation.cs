// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account;

public class ResendEmailConfirmation : DefaultUIPage
{
    private readonly IHtmlFormElement _resendForm;

    public ResendEmailConfirmation(HttpClient client, IHtmlDocument document, DefaultUIContext context) : base(client, document, context)
    {
        _resendForm = HtmlAssert.HasForm(document);
    }

    public Task<HttpResponseMessage> ResendAsync(string email)
        => Client.SendAsync(_resendForm, new Dictionary<string, string>
        {
            ["Input_Email"] = email
        });
}
