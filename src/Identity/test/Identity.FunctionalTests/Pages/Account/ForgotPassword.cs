// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account;

public class ForgotPassword : DefaultUIPage
{
    private readonly IHtmlFormElement _forgotPasswordForm;

    public ForgotPassword(HttpClient client, IHtmlDocument document, DefaultUIContext context) : base(client, document, context)
    {
        _forgotPasswordForm = HtmlAssert.HasForm(document);
    }

    public async Task<ForgotPasswordConfirmation> SendForgotPasswordAsync(string email)
    {
        var response = await Client.SendAsync(_forgotPasswordForm, new Dictionary<string, string>
        {
            ["Input_Email"] = email
        });
        var goToForgotPasswordConfirmation = ResponseAssert.IsRedirect(response);
        var forgotPasswordConfirmationResponse = await Client.GetAsync(goToForgotPasswordConfirmation);
        var forgotPasswordConfirmation = await ResponseAssert.IsHtmlDocumentAsync(forgotPasswordConfirmationResponse);

        return new ForgotPasswordConfirmation(Client, forgotPasswordConfirmation, Context);
    }
}
