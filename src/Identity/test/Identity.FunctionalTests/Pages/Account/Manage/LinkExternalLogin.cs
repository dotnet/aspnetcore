// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Html.Dom;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class LinkExternalLogin : DefaultUIPage
{
    private readonly IHtmlFormElement _confirmPasswordForm;
    private readonly IHtmlFormElement _confirmExternalLoginForm;
    private readonly IHtmlFormElement _linkLoginForm;

    public LinkExternalLogin(HttpClient client, IHtmlDocument externalLoginsDocument, DefaultUIContext context)
        : base(client, externalLoginsDocument, context)
    {
        _confirmPasswordForm = externalLoginsDocument.QuerySelector("#confirm-password-form") as IHtmlFormElement;
        _confirmExternalLoginForm = externalLoginsDocument.QuerySelector("#confirm-external-login-form") as IHtmlFormElement;
        _linkLoginForm = externalLoginsDocument.QuerySelector("#link-login-form") as IHtmlFormElement;
    }

    public async Task AssertLinkingRequiresReauthenticationAsync()
    {
        Assert.NotNull(_confirmPasswordForm);
        Assert.Null(_linkLoginForm);
        Assert.Null(Document.QuerySelector("#link-login-button-Contoso"));

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

    public async Task<LinkExternalLogin> ConfirmExternalLoginAsync(string provider, string login)
    {
        Assert.NotNull(_confirmExternalLoginForm);
        var providerButton = Assert.IsAssignableFrom<IHtmlElement>(
            _confirmExternalLoginForm.QuerySelector($"button[value={provider}]"));

        var challengeResponse = await Client.SendAsync(_confirmExternalLoginForm, providerButton);
        var providerRedirect = ResponseAssert.IsRedirect(challengeResponse);
        var providerResponse = await Client.GetAsync(providerRedirect);
        var providerDocument = await ResponseAssert.IsHtmlDocumentAsync(providerResponse);
        var providerForm = HtmlAssert.HasForm("#external-login", providerDocument);

        var callbackRedirectResponse = await Client.SendAsync(providerForm, new Dictionary<string, string>
        {
            ["Input_Login"] = login,
        });
        var callbackRedirect = ResponseAssert.IsRedirect(callbackRedirectResponse);
        var callbackResponse = await Client.GetAsync(callbackRedirect);
        var manageRedirect = ResponseAssert.IsRedirect(callbackResponse);
        var manageResponse = await Client.GetAsync(manageRedirect);
        var manageDocument = await ResponseAssert.IsHtmlDocumentAsync(manageResponse);

        return new LinkExternalLogin(Client, manageDocument, Context);
    }

    public async Task<ManageExternalLogin> BeginLinkExternalLoginAsync(string provider = "Contoso")
    {
        Assert.Null(_confirmPasswordForm);
        Assert.NotNull(_linkLoginForm);
        var linkLoginButton = Assert.IsAssignableFrom<IHtmlElement>(
            _linkLoginForm.QuerySelector($"#link-login-button-{provider}"));

        // Click on the button to link external login to current user account
        var linkExternalLogin = await Client.SendAsync(_linkLoginForm, linkLoginButton);
        var goToLinkExternalLogin = ResponseAssert.IsRedirect(linkExternalLogin);
        var externalLoginResponse = await Client.GetAsync(goToLinkExternalLogin);
        var externalLoginDocument = await ResponseAssert.IsHtmlDocumentAsync(externalLoginResponse);

        // Redirected to manage page for external login with a remove button
        return new ManageExternalLogin(Client, externalLoginDocument, Context);
    }

    public Task<HttpResponseMessage> ClearReauthenticationAsync() =>
        Client.PostAsync("/test/clear-identity-ui-reauthentication", content: null);

    public RemoveExternalLogin ClickRemoveLoginAsync(IHtmlDocument linkedExternalLoginDocument)
    {
        return new RemoveExternalLogin(Client, linkedExternalLoginDocument, Context);
    }
}
