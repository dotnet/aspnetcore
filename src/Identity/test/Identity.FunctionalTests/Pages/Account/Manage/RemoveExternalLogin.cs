// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class RemoveExternalLogin : DefaultUIPage
    {
        private readonly IHtmlFormElement _removeLoginForm;

        public RemoveExternalLogin(HttpClient client, IHtmlDocument externalLogin, DefaultUIContext context)
            : base(client, externalLogin, context)
        {
            _removeLoginForm = HtmlAssert.HasForm($"#remove-login-Contoso", externalLogin);
        }

        public async Task<RemoveExternalLogin> RemoveLoginAsync(string loginProvider, string providerKey)
        {
            await Client.SendAsync(_removeLoginForm, new Dictionary<string, string>
            {
                ["login_LoginProvider"] = loginProvider,
                ["login_ProviderKey"] = providerKey
            });

            return this;
        }
    }
}