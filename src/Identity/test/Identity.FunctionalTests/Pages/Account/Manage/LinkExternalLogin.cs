// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Html.Dom;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class LinkExternalLogin : DefaultUIPage
{
    private readonly IHtmlFormElement _confirmPasswordForm;
    private readonly IHtmlFormElement _linkLoginForm;
    private readonly IHtmlElement _linkLoginButton;

    public LinkExternalLogin(HttpClient client, IHtmlDocument externalLoginsDocument, DefaultUIContext context)
        : base(client, externalLoginsDocument, context)
    {
        _confirmPasswordForm = externalLoginsDocument.QuerySelector("#confirm-password-form") as IHtmlFormElement;
        _linkLoginForm = externalLoginsDocument.QuerySelector("#link-login-form") as IHtmlFormElement;
        _linkLoginButton = externalLoginsDocument.QuerySelector("#link-login-button-Contoso") as IHtmlElement;
    }

    public async Task AssertLinkingRequiresReauthenticationAsync()
    {
        Assert.NotNull(_confirmPasswordForm);
        Assert.Null(_linkLoginForm);
        Assert.Null(_linkLoginButton);

        var antiforgeryToken = Assert.IsAssignableFrom<IHtmlInputElement>(
            _confirmPasswordForm.QuerySelector("input[name=__RequestVerificationToken]"));
        var response = await Client.PostAsync(
            _confirmPasswordForm.Action,
            new FormUrlEncodedContent(
            [
                new("__RequestVerificationToken", antiforgeryToken.Value),
                new("provider", "Contoso"),
            ]));
        var redirect = ResponseAssert.IsRedirect(response);
        var redirectedResponse = await Client.GetAsync(redirect);
        var redirectedDocument = await ResponseAssert.IsHtmlDocumentAsync(redirectedResponse);
        Assert.Contains("You must confirm your identity before adding an external login.", redirectedDocument.Body.TextContent);
    }

    public async Task<LinkExternalLogin> ConfirmPasswordAsync(string password)
    {
        Assert.NotNull(_confirmPasswordForm);

        var response = await Client.SendAsync(_confirmPasswordForm, new Dictionary<string, string>
        {
            ["password"] = password,
        });
        var redirect = ResponseAssert.IsRedirect(response);
        var redirectedResponse = await Client.GetAsync(redirect);
        var redirectedDocument = await ResponseAssert.IsHtmlDocumentAsync(redirectedResponse);

        return new LinkExternalLogin(Client, redirectedDocument, Context);
    }

    public async Task<ManageExternalLogin> LinkExternalLoginAsync(string loginEmail)
    {
        Assert.Null(_confirmPasswordForm);
        Assert.NotNull(_linkLoginForm);
        Assert.NotNull(_linkLoginButton);

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
