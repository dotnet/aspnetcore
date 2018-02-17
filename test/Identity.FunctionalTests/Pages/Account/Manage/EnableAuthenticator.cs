// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    internal class EnableAuthenticator : DefaultUIPage
    {
        public const string AuthenticatorKey = nameof(EnableAuthenticator) + "." + nameof(AuthenticatorKey);

        private readonly IHtmlElement _codeElement;
        private readonly IHtmlFormElement _sendCodeForm;

        public EnableAuthenticator(
            HttpClient client,
            IHtmlDocument enableAuthenticator,
            DefaultUIContext context)
            : base(client, enableAuthenticator, context)
        {
            Assert.True(Context.UserAuthenticated);
            _codeElement = HtmlAssert.HasElement("kbd", enableAuthenticator);
            _sendCodeForm = HtmlAssert.HasForm("#send-code", enableAuthenticator);
        }

        internal async Task<ShowRecoveryCodes> SendValidCodeAsync()
        {
            var authenticatorKey = _codeElement.TextContent.Replace(" ", "");
            Context.AuthenticatorKey = authenticatorKey;
            var verificationCode = ComputeCode(authenticatorKey);

            var sendCodeResponse = await Client.SendAsync(_sendCodeForm, new Dictionary<string, string>
            {
                ["Input_Code"] = verificationCode
            });

            var goToShowRecoveryCodes = ResponseAssert.IsRedirect(sendCodeResponse);
            var showRecoveryCodesResponse = await Client.GetAsync(goToShowRecoveryCodes);
            var showRecoveryCodes = await ResponseAssert.IsHtmlDocumentAsync(showRecoveryCodesResponse);

            return new ShowRecoveryCodes(Client, showRecoveryCodes, Context);
        }

        public static string ComputeCode(string key)
        {
            var hash = new HMACSHA1(Base32.FromBase32(key));
            var unixTimestamp = Convert.ToInt64(Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds));
            var timestep = Convert.ToInt64(unixTimestamp / 30);
            var topt = Rfc6238AuthenticationService.ComputeTotp(hash, (ulong)timestep, modifier: null);
            return topt.ToString("D6");
        }
    }
}