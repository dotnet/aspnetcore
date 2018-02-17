// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class TwoFactorAuthentication : DefaultUIPage
    {
        private readonly IHtmlAnchorElement _enableAuthenticatorLink;

        public TwoFactorAuthentication(HttpClient client, IHtmlDocument twoFactor, DefaultUIContext context)
            : base(client, twoFactor, context)
        {
            if (!Context.TwoFactorEnabled)
            {
                _enableAuthenticatorLink = HtmlAssert.HasLink("#enable-authenticator", twoFactor);
            }
        }

        internal async Task<EnableAuthenticator> ClickEnableAuthenticatorLinkAsync()
        {
            Assert.False(Context.TwoFactorEnabled);

            var goToEnableAuthenticator = await Client.GetAsync(_enableAuthenticatorLink.Href);
            var enableAuthenticator = await ResponseAssert.IsHtmlDocumentAsync(goToEnableAuthenticator);

            return new EnableAuthenticator(Client, enableAuthenticator, Context);
        }
    }
}