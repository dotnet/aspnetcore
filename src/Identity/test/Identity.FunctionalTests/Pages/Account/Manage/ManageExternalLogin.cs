// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class ManageExternalLogin : DefaultUIPage
{
    private readonly IHtmlFormElement _externalLoginForm;

    public ManageExternalLogin(HttpClient client, IHtmlDocument externalLoginDocument, DefaultUIContext context)
        : base(client, externalLoginDocument, context)
    {
        _externalLoginForm = HtmlAssert.HasForm("#external-login", externalLoginDocument);
    }

    public async Task<RemoveExternalLogin> ManageExternalLoginAsync(string loginEmail)
    {
        var linkedExternalLogin = await Client.SendAsync(_externalLoginForm, new Dictionary<string, string>
        {
            ["Input_Login"] = loginEmail
        });

        var goToLinkedExternalLogin = ResponseAssert.IsRedirect(linkedExternalLogin);
        var externalLoginResponse = await Client.GetAsync(goToLinkedExternalLogin);
        var goToManageExternalLogin = ResponseAssert.IsRedirect(externalLoginResponse);
        var manageExternalLoginResponse = await Client.GetAsync(goToManageExternalLogin);

        var manageExternalLoginDocument = await ResponseAssert.IsHtmlDocumentAsync(manageExternalLoginResponse);
        return new RemoveExternalLogin(Client, manageExternalLoginDocument, Context);
    }
}
