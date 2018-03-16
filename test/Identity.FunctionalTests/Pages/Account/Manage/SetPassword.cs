// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class SetPassword : DefaultUIPage
    {
        private readonly IHtmlFormElement _setPasswordForm;

        public SetPassword(HttpClient client, IHtmlDocument setPassword, DefaultUIContext context)
            : base(client, setPassword, context)
        {
            _setPasswordForm = HtmlAssert.HasForm("#set-password-form", setPassword);
        }

        public async Task<SetPassword> SetPasswordAsync(string newPassword)
        {
            await Client.SendAsync(_setPasswordForm, new Dictionary<string, string>
            {
                ["Input_NewPassword"] = newPassword,
                ["Input_ConfirmPassword"] = newPassword
            });

            return this;
        }
    }
}
