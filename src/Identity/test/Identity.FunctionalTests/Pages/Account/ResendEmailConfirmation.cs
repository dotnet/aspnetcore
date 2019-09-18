// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class ResendEmailConfirmation : DefaultUIPage
    {
        private readonly IHtmlFormElement _resendForm;

        public ResendEmailConfirmation(HttpClient client, IHtmlDocument document, DefaultUIContext context) : base(client, document, context)
        {
            _resendForm = HtmlAssert.HasForm(document);
        }

        public Task<HttpResponseMessage> ResendAsync(string email)
            => Client.SendAsync(_resendForm, new Dictionary<string, string>
            {
                ["Input_Email"] = email
            });
    }
}
