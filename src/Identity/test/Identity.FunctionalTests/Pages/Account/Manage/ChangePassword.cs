// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage
{
    public class ChangePassword : DefaultUIPage
    {
        private readonly IHtmlFormElement _changePasswordForm;

        public ChangePassword(HttpClient client, IHtmlDocument changePassword, DefaultUIContext context)
            : base(client, changePassword, context)
        {
            _changePasswordForm = HtmlAssert.HasForm("#change-password-form", changePassword);
        }

        public async Task<ChangePassword> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            await Client.SendAsync(_changePasswordForm, new Dictionary<string, string>
            {
                ["Input_OldPassword"] = oldPassword,
                ["Input_NewPassword"] = newPassword,
                ["Input_ConfirmPassword"] = newPassword
            });

            return this;
        }
    }
}
