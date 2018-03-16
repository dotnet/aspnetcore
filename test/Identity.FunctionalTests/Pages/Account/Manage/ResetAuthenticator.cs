// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class ResetAuthenticator : DefaultUIPage
    {
        private readonly IHtmlFormElement _resetAuthenticatorForm;
        private readonly IHtmlElement _resetAuthenticatorButton;

        public ResetAuthenticator(
            HttpClient client,
            IHtmlDocument resetAuthenticator,
            DefaultUIContext context)
            : base(client, resetAuthenticator, context)
        {
            Assert.True(Context.UserAuthenticated);
            _resetAuthenticatorForm = HtmlAssert.HasForm("#reset-authenticator-form", resetAuthenticator);
            _resetAuthenticatorButton = HtmlAssert.HasElement("#reset-authenticator-button", resetAuthenticator);
        }

        internal async Task<ResetAuthenticator> ResetAuthenticatorAsync()
        {
            await Client.SendAsync(_resetAuthenticatorForm, _resetAuthenticatorButton);
            return this;
        }
    }
}