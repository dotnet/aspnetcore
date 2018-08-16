// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var title = externalLogin.GetElementsByTagName("h4").FirstOrDefault(e => e.TextContent.StartsWith("Associate your"));
            Assert.Equal("Associate your Contoso auth account.", title?.TextContent);
            var info = externalLogin.QuerySelectorAll<IHtmlParagraphElement>(".text-info").FirstOrDefault(e => e.TextContent.Trim().StartsWith("You've successfully authenticated"));
            Assert.StartsWith("You've successfully authenticated with Contoso auth.", info?.TextContent.Trim());
        }

        public async Task<Index> SendEmailAsync(string email)
        {
            var response = await Client.SendAsync(_emailForm, new Dictionary<string, string>
            {
                ["Input_Email"] = email
            });
            var goToIndex = ResponseAssert.IsRedirect(response);
            var indexResponse = await Client.GetAsync(goToIndex);
            var index = await ResponseAssert.IsHtmlDocumentAsync(indexResponse);

            return new Index(Client, index, Context.WithAuthenticatedUser());
        }
    }
}
