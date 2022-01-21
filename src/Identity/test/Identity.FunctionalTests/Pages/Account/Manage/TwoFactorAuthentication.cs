// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class TwoFactorAuthentication : DefaultUIPage
{
    private readonly IHtmlAnchorElement _enableAuthenticatorLink;
    private readonly IHtmlAnchorElement _resetAuthenticatorLink;

    public TwoFactorAuthentication(HttpClient client, IHtmlDocument twoFactor, DefaultUIContext context)
        : base(client, twoFactor, context)
    {
        if (Context.CookiePolicyAccepted)
        {
            if (!Context.TwoFactorEnabled)
            {
                _enableAuthenticatorLink = HtmlAssert.HasLink("#enable-authenticator", twoFactor);
            }
            else
            {
                _resetAuthenticatorLink = HtmlAssert.HasLink("#reset-authenticator", twoFactor);
            }
        }
        else
        {
            Assert.Contains("You must accept the policy before you can enable two factor authentication.", twoFactor.DocumentElement.TextContent);
        }
    }

    internal async Task<EnableAuthenticator> ClickEnableAuthenticatorLinkAsync()
    {
        Assert.False(Context.TwoFactorEnabled);

        var goToEnableAuthenticator = await Client.GetAsync(_enableAuthenticatorLink.Href);
        var enableAuthenticator = await ResponseAssert.IsHtmlDocumentAsync(goToEnableAuthenticator);

        return new EnableAuthenticator(Client, enableAuthenticator, Context);
    }

    internal async Task<ResetAuthenticator> ClickResetAuthenticatorLinkAsync()
    {
        var goToResetAuthenticator = await Client.GetAsync(_resetAuthenticatorLink.Href);
        var resetAuthenticator = await ResponseAssert.IsHtmlDocumentAsync(goToResetAuthenticator);

        return new ResetAuthenticator(Client, resetAuthenticator, Context);
    }
}
