// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account
{
    public class Register : DefaultUIPage
    {
        private IHtmlFormElement _registerForm;

        public Register(HttpClient client, IHtmlDocument register, DefaultUIContext context)
            : base(client, register, context)
        {
            _registerForm = HtmlAssert.HasForm(register);
        }

        public async Task<Index> SubmitRegisterFormForValidUserAsync(string userName, string password)
        {
            var registered = await Client.SendAsync(_registerForm, new Dictionary<string, string>()
            {
                ["Input_Email"] = userName,
                ["Input_Password"] = password,
                ["Input_ConfirmPassword"] = password
            });

            var registeredLocation = ResponseAssert.IsRedirect(registered);
            Assert.Equal(Index.Path, registeredLocation.ToString());
            var indexResponse = await Client.GetAsync(registeredLocation);
            var index = await ResponseAssert.IsHtmlDocumentAsync(indexResponse);

            return new Index(Client, index, Context.WithAuthenticatedUser());
        }
    }
}
