// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class RemoveExternalLogin : DefaultUIPage
{
    private readonly IHtmlFormElement _removeLoginForm;

    public RemoveExternalLogin(HttpClient client, IHtmlDocument externalLogin, DefaultUIContext context)
        : base(client, externalLogin, context)
    {
        _removeLoginForm = HtmlAssert.HasForm($"#remove-login-Contoso", externalLogin);
    }

    public async Task<RemoveExternalLogin> RemoveLoginAsync(string loginProvider, string providerKey)
    {
        await Client.SendAsync(_removeLoginForm, new Dictionary<string, string>
        {
            ["login_LoginProvider"] = loginProvider,
            ["login_ProviderKey"] = providerKey
        });

        return this;
    }
}
