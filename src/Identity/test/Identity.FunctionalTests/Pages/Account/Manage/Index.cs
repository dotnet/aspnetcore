// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class Index : DefaultUIPage
    {
        private readonly IHtmlAnchorElement _profileLink;
        private readonly IHtmlAnchorElement _changePasswordLink;
        private readonly IHtmlAnchorElement _twoFactorLink;
        private readonly IHtmlAnchorElement _externalLoginLink;
        private readonly IHtmlAnchorElement _personalDataLink;
        private readonly IHtmlFormElement _updateProfileForm;
        private readonly IHtmlElement _emailInput;
        private readonly IHtmlElement _userNameInput;
        private readonly IHtmlElement _updateProfileButton;
        private readonly IHtmlElement _confirmEmailButton;
        public static readonly string Path = "/";

        public Index(HttpClient client, IHtmlDocument manage, DefaultUIContext context)
            : base(client, manage, context)
        {
            Assert.True(Context.UserAuthenticated);

            _profileLink = HtmlAssert.HasLink("#profile", manage);
            _changePasswordLink = HtmlAssert.HasLink("#change-password", manage);
            _twoFactorLink = HtmlAssert.HasLink("#two-factor", manage);
            if (Context.ContosoLoginEnabled)
            {
                _externalLoginLink = HtmlAssert.HasLink("#external-login", manage);
            }
            _personalDataLink = HtmlAssert.HasLink("#personal-data", manage);
            _updateProfileForm = HtmlAssert.HasForm("#profile-form", manage);
            _emailInput = HtmlAssert.HasElement("#Input_Email", manage);
            _userNameInput = HtmlAssert.HasElement("#Username", manage);
            _updateProfileButton = HtmlAssert.HasElement("#update-profile-button", manage);
            if (!Context.EmailConfirmed)
            {
                _confirmEmailButton = HtmlAssert.HasElement("button#email-verification", manage);
            }
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

        internal async Task<Index> SendConfirmationEmailAsync()
        {
            Assert.False(Context.EmailConfirmed);

            var response = await Client.SendAsync(_updateProfileForm, _confirmEmailButton);
            var goToManage = ResponseAssert.IsRedirect(response);
            var manageResponse = await Client.GetAsync(goToManage);
            var manage = await ResponseAssert.IsHtmlDocumentAsync(manageResponse);

            return new Index(Client, manage, Context);
        }

        internal async Task<Index> SendUpdateProfileAsync(string newEmail)
        {
            var response = await Client.SendAsync(_updateProfileForm, _updateProfileButton, new Dictionary<string, string>
            {
                ["Input_Email"] = newEmail
            });
            var goToManage = ResponseAssert.IsRedirect(response);
            var manageResponse = await Client.GetAsync(goToManage);
            var manage = await ResponseAssert.IsHtmlDocumentAsync(manageResponse);

            return new Index(Client, manage, Context);
        }

        public async Task<ChangePassword> ClickChangePasswordLinkAsync()
        {
            var goToChangePassword = await Client.GetAsync(_changePasswordLink.Href);
            var changePasswordDocument = await ResponseAssert.IsHtmlDocumentAsync(goToChangePassword);
            return new ChangePassword(Client, changePasswordDocument, Context);
        }

        internal string GetEmail() => _emailInput.GetAttribute("value");

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
}
