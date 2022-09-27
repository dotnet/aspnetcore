// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class Index : DefaultUIPage
{
    private readonly IHtmlAnchorElement _profileLink;
    private readonly IHtmlAnchorElement _emailLink;
    private readonly IHtmlAnchorElement _changePasswordLink;
    private readonly IHtmlAnchorElement _twoFactorLink;
    private readonly IHtmlAnchorElement _externalLoginLink;
    private readonly IHtmlAnchorElement _personalDataLink;
    private readonly IHtmlFormElement _updateProfileForm;
    private readonly IHtmlElement _userNameInput;
    private readonly IHtmlElement _updateProfileButton;
    public static readonly string Path = "/";

    public Index(HttpClient client, IHtmlDocument manage, DefaultUIContext context)
        : base(client, manage, context)
    {
        Assert.True(Context.UserAuthenticated);

        _profileLink = HtmlAssert.HasLink("#profile", manage);
        _emailLink = HtmlAssert.HasLink("#email", manage);
        _changePasswordLink = HtmlAssert.HasLink("#change-password", manage);
        _twoFactorLink = HtmlAssert.HasLink("#two-factor", manage);
        if (Context.ContosoLoginEnabled)
        {
            _externalLoginLink = HtmlAssert.HasLink("#external-login", manage);
        }
        _personalDataLink = HtmlAssert.HasLink("#personal-data", manage);
        _updateProfileForm = HtmlAssert.HasForm("#profile-form", manage);
        _userNameInput = HtmlAssert.HasElement("#Username", manage);
        _updateProfileButton = HtmlAssert.HasElement("#update-profile-button", manage);
    }

    public async Task<TwoFactorAuthentication> ClickTwoFactorLinkAsync(bool consent = true)
    {
        // Accept cookie consent if requested
        if (consent)
        {
            await UserStories.AcceptCookiePolicy(Client);
        }

        var goToTwoFactor = await Client.GetAsync(_twoFactorLink.Href);
        var twoFactor = await ResponseAssert.IsHtmlDocumentAsync(goToTwoFactor);

        var context = consent ? Context.WithCookieConsent() : Context;
        return new TwoFactorAuthentication(Client, twoFactor, context);
    }

    public async Task<TwoFactorAuthentication> ClickTwoFactorEnabledLinkAsync()
    {
        var goToTwoFactor = await Client.GetAsync(_twoFactorLink.Href);
        var twoFactor = await ResponseAssert.IsHtmlDocumentAsync(goToTwoFactor);
        Context.TwoFactorEnabled = true;
        Context.CookiePolicyAccepted = true;
        return new TwoFactorAuthentication(Client, twoFactor, Context);
    }

    public async Task<ChangePassword> ClickChangePasswordLinkAsync()
    {
        var goToChangePassword = await Client.GetAsync(_changePasswordLink.Href);
        var changePasswordDocument = await ResponseAssert.IsHtmlDocumentAsync(goToChangePassword);
        return new ChangePassword(Client, changePasswordDocument, Context);
    }

    internal object GetUserName() => _userNameInput.GetAttribute("value");

    public async Task<SetPassword> ClickChangePasswordLinkExternalLoginAsync()
    {
        var response = await Client.GetAsync(_changePasswordLink.Href);
        var goToSetPassword = ResponseAssert.IsRedirect(response);
        var setPasswordResponse = await Client.GetAsync(goToSetPassword);
        var setPasswordDocument = await ResponseAssert.IsHtmlDocumentAsync(setPasswordResponse);
        return new SetPassword(Client, setPasswordDocument, Context);
    }

    public async Task<PersonalData> ClickPersonalDataLinkAsync()
    {
        var goToPersonalData = await Client.GetAsync(_personalDataLink.Href);
        var personalData = await ResponseAssert.IsHtmlDocumentAsync(goToPersonalData);
        return new PersonalData(Client, personalData, Context);
    }

    public async Task<Email> ClickEmailLinkAsync()
    {
        var goToEmail = await Client.GetAsync(_emailLink.Href);
        var email = await ResponseAssert.IsHtmlDocumentAsync(goToEmail);
        return new Email(Client, email, Context);
    }

    public async Task<LinkExternalLogin> ClickLinkLoginAsync()
    {
        var goToExternalLogin = await Client.GetAsync(_externalLoginLink.Href);
        var externalLoginDocument = await ResponseAssert.IsHtmlDocumentAsync(goToExternalLogin);

        return new LinkExternalLogin(Client, externalLoginDocument, Context);
    }

    public async Task<ExternalLogins> ClickExternalLoginsAsync()
    {
        var goToExternalLogin = await Client.GetAsync(_externalLoginLink.Href);
        var externalLoginDocument = await ResponseAssert.IsHtmlDocumentAsync(goToExternalLogin);

        return new ExternalLogins(Client, externalLoginDocument, Context);
    }
}
