// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class ExternalLogin : DefaultUIPage
    {
        private readonly IHtmlFormElement _emailForm;

        public ExternalLogin(
            HttpClient client,
            IHtmlDocument externalLogin,
            DefaultUIContext context)
            : base(client, externalLogin, context)
        {
            _emailForm = HtmlAssert.HasForm(Document);
            Title = HtmlAssert.HasElement("#external-login-title", Document);
            Description = HtmlAssert.HasElement("#external-login-description",Document);
        }

        public IHtmlElement Title { get; }
        public IHtmlElement Description { get; }

        public async Task<Index> SendEmailAsync(string email)
        {
            var response = await Client.SendAsync(_emailForm, new Dictionary<string, string>
            {
                ["Input_Email"] = email
            });
            var redirect = ResponseAssert.IsRedirect(response);
            var indexResponse = await Client.GetAsync(redirect);
            var index = await ResponseAssert.IsHtmlDocumentAsync(indexResponse);
            return new Index(Client, index, Context.WithAuthenticatedUser());
        }

        public async Task<RegisterConfirmation> SendEmailWithConfirmationAsync(string email, bool hasRealEmail)
        {
            var response = await Client.SendAsync(_emailForm, new Dictionary<string, string>
            {
                ["Input_Email"] = email
            });
            var redirect = ResponseAssert.IsRedirect(response);
            Assert.Equal(RegisterConfirmation.Path + "?email=" + email, redirect.ToString(), StringComparer.OrdinalIgnoreCase);

            var registerResponse = await Client.GetAsync(redirect);
            var register = await ResponseAssert.IsHtmlDocumentAsync(registerResponse);

            return new RegisterConfirmation(Client, register, hasRealEmail ? Context.WithRealEmailSender() : Context);
        }
    }
}
