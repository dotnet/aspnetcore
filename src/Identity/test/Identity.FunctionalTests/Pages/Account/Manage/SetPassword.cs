// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

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
