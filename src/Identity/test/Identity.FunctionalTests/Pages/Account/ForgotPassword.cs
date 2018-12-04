// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class ForgotPassword : DefaultUIPage
    {
        private readonly IHtmlFormElement _forgotPasswordForm;

        public ForgotPassword(HttpClient client, IHtmlDocument document, DefaultUIContext context) : base(client, document, context)
        {
            _forgotPasswordForm = HtmlAssert.HasForm(document);
        }

        public async Task<ForgotPasswordConfirmation> SendForgotPasswordAsync(string email)
        {
            var response = await Client.SendAsync(_forgotPasswordForm, new Dictionary<string, string>
            {
                ["Input_Email"] = email
            });
            var goToForgotPasswordConfirmation = ResponseAssert.IsRedirect(response);
            var forgotPasswordConfirmationResponse = await Client.GetAsync(goToForgotPasswordConfirmation);
            var forgotPasswordConfirmation = await ResponseAssert.IsHtmlDocumentAsync(forgotPasswordConfirmationResponse);

            return new ForgotPasswordConfirmation(Client, forgotPasswordConfirmation, Context);
        }
    }
}
