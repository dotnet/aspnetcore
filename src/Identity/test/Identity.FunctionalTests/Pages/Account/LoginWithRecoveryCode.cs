// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class LoginWithRecoveryCode : DefaultUIPage
    {
        private readonly IHtmlFormElement _loginWithRecoveryCodeForm;

        public LoginWithRecoveryCode(HttpClient client, IHtmlDocument loginWithRecoveryCode, DefaultUIContext context)
            : base(client, loginWithRecoveryCode, context)
        {
            _loginWithRecoveryCodeForm = HtmlAssert.HasForm(loginWithRecoveryCode);
        }

        public async Task<Index> SendRecoveryCodeAsync(string recoveryCode)
        {
            var response = await Client.SendAsync(_loginWithRecoveryCodeForm, new Dictionary<string, string>
            {
                ["Input_RecoveryCode"] = recoveryCode
            });

            var goToIndex = ResponseAssert.IsRedirect(response);
            var indexPage = await Client.GetAsync(goToIndex);
            var index = await ResponseAssert.IsHtmlDocumentAsync(indexPage);

            return new Index(Client, index, Context.WithAuthenticatedUser());
        }
    }
}