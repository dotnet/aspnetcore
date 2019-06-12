// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Microsoft.AspNetCore.Identity.FunctionalTests.Account;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class RegisterConfirmation : DefaultUIPage
    {
        private readonly IHtmlAnchorElement _confirmLink;
        public static readonly string Path = "/Identity/Account/RegisterConfirmation";

        public RegisterConfirmation(
            HttpClient client,
            IHtmlDocument register,
            DefaultUIContext context)
            : base(client, register, context)
        {
            if (Context.HasRealEmailSender)
            {
                Assert.Empty(Document.QuerySelectorAll("#confirm-link"));
            }
            else
            {
                _confirmLink = HtmlAssert.HasLink("#confirm-link", Document);
            }
        }

        public async Task<ConfirmEmail> ClickConfirmLinkAsync()
        {
            var goToConfirm = await Client.GetAsync(_confirmLink.Href);
            var confirm = await ResponseAssert.IsHtmlDocumentAsync(goToConfirm);

            return await ConfirmEmail.Create(_confirmLink, Client, Context);
        }
    }
}
