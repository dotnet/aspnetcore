// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Microsoft.AspNetCore.Identity.FunctionalTests.Account;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class Index : HtmlPage
    {
        private readonly bool _authenticated;
        private readonly IHtmlAnchorElement _registerLink;
        private readonly IHtmlAnchorElement _loginLink;
        private readonly IHtmlAnchorElement _manageLink;
        public static readonly string Path = "/";

        public Index(HttpClient client, IHtmlDocument index, HtmlPageContext context, bool authenticated)
            : base(client, index, context)
        {
            _authenticated = authenticated;
            if (!_authenticated)
            {
                _registerLink = HtmlAssert.HasLink("#register", Document);
                _loginLink = HtmlAssert.HasLink("#login", Document);
            }
            else
            {
                _manageLink = HtmlAssert.HasLink("#manage", Document);
            }
        }

        public static async Task<Index> CreateAsync(HttpClient client, bool authenticated = false)
        {
            var goToIndex = await client.GetAsync("/");
            var index = await ResponseAssert.IsHtmlDocumentAsync(goToIndex);

            return new Index(client, index, new HtmlPageContext(), authenticated);
        }

        public async Task<Register> ClickRegisterLinkAsync()
        {
            Assert.False(_authenticated);

            var goToRegister = await Client.GetAsync(_registerLink.Href);
            var register = await ResponseAssert.IsHtmlDocumentAsync(goToRegister);

            return new Register(Client, register, Context);
        }

        public async Task<Login> ClickLoginLinkAsync()
        {
            Assert.False(_authenticated);

            var goToLogin = await Client.GetAsync(_loginLink.Href);
            var login = await ResponseAssert.IsHtmlDocumentAsync(goToLogin);

            return new Login(Client, login, Context);
        }

        internal async Task<Account.Manage.Index> ClickManageLinkAsync()
        {
            Assert.True(_authenticated);

            var goToManage = await Client.GetAsync(_manageLink.Href);
            var manage = await ResponseAssert.IsHtmlDocumentAsync(goToManage);

            return new Account.Manage.Index(Client, manage, Context);
        }
    }
}
