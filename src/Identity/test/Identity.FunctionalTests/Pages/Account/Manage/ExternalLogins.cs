// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class ExternalLogins : DefaultUIPage
    {
        public ExternalLogins(HttpClient client, IHtmlDocument externalLoginDocument, DefaultUIContext context)
            : base(client, externalLoginDocument, context)
        {
            if (context.SocialLoginProvider != null && context.PasswordLoginEnabled)
            {
                RemoveExternalLogin = HtmlAssert.HasForm("#remove-login-Contoso", Document);
            }

            if (context.SocialLoginProvider != null)
            {
                ExternalLoginDisplayName = HtmlAssert.HasElement("#login-provider-Contoso", Document);
            }
        }

        public IHtmlFormElement RemoveExternalLogin { get; }
        public IHtmlElement ExternalLoginDisplayName { get; }
    }
}