// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    internal class ShowRecoveryCodes : DefaultUIPage
    {
        private readonly IEnumerable<IHtmlElement> _recoveryCodeElements;

        public ShowRecoveryCodes(HttpClient client, IHtmlDocument showRecoveryCodes, DefaultUIContext context)
            : base(client, showRecoveryCodes, context)
        {
            _recoveryCodeElements = HtmlAssert.HasElements(".recovery-code", showRecoveryCodes);
            Context.RecoveryCodes = Codes.ToArray();
        }

        public IEnumerable<string> Codes => _recoveryCodeElements.Select(rc => rc.TextContent);
    }
}