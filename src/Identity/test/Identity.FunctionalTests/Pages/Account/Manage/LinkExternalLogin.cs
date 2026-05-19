// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class LinkExternalLogin : DefaultUIPage
{
    private readonly IHtmlFormElement _linkLoginForm;
    private readonly IHtmlElement _linkLoginButton;

    public LinkExternalLogin(HttpClient client, IHtmlDocument externalLoginsDocument, DefaultUIContext context)
        : base(client, externalLoginsDocument, context)
    {
        _linkLoginForm = HtmlAssert.HasForm($"#link-login-form", externalLoginsDocument);
        _linkLoginButton = HtmlAssert.HasElement($"#link-login-button-Contoso", externalLoginsDocument);
    }

    public async Task<ManageExternalLogin> LinkExternalLoginAsync(string loginEmail)
    {
        // Click on the button to link external login to current user account
        var linkExternalLogin = await Client.SendAsync(_linkLoginForm, _linkLoginButton);
        var goToLinkExternalLogin = ResponseAssert.IsRedirect(linkExternalLogin);
        var externalLoginResponse = await Client.GetAsync(goToLinkExternalLogin);
        var externalLoginDocument = await ResponseAssert.IsHtmlDocumentAsync(externalLoginResponse);

        // Redirected to manage page for external login with a remove button
        return new ManageExternalLogin(Client, externalLoginDocument, Context);
    }

    public RemoveExternalLogin ClickRemoveLoginAsync(IHtmlDocument linkedExternalLoginDocument)
    {
        return new RemoveExternalLogin(Client, linkedExternalLoginDocument, Context);
    }
}
