// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class Index : HtmlPage
    {
        private readonly IHtmlAnchorElement _profileLink;
        private readonly IHtmlAnchorElement _changePasswordLink;
        private readonly IHtmlAnchorElement _twoFactorLink;
        private readonly IHtmlAnchorElement _personalDataLink;

        public Index(HttpClient client, IHtmlDocument manage, HtmlPageContext context)
            : base(client, manage, context)
        {
            _profileLink = HtmlAssert.HasLink("#profile", manage);
            _changePasswordLink = HtmlAssert.HasLink("#change-password", manage);
            _twoFactorLink = HtmlAssert.HasLink("#two-factor", manage);
            _personalDataLink = HtmlAssert.HasLink("#personal-data", manage);
        }

        public async Task<TwoFactorAuthentication> ClickTwoFactorLinkAsync(bool twoFactorEnabled)
        {
            var goToTwoFactor = await Client.GetAsync(_twoFactorLink.Href);
            var twoFactor = await ResponseAssert.IsHtmlDocumentAsync(goToTwoFactor);

            return new TwoFactorAuthentication(Client, twoFactor, Context, twoFactorEnabled);
        }
    }
}
