// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.WebUtilities;

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
        var manageExternalLoginDocument = await CompleteExternalLoginAsync(loginEmail);
        return new RemoveExternalLogin(Client, manageExternalLoginDocument, Context);
    }

    public async Task<IHtmlDocument> CompleteExternalLoginAsync(string loginEmail)
    {
        var linkedExternalLogin = await Client.SendAsync(_externalLoginForm, new Dictionary<string, string>
        {
            ["Input_Login"] = loginEmail
        });

        var goToLinkedExternalLogin = ResponseAssert.IsRedirect(linkedExternalLogin);
        var externalLoginResponse = await Client.GetAsync(goToLinkedExternalLogin);
        var goToManageExternalLogin = ResponseAssert.IsRedirect(externalLoginResponse);
        var manageExternalLoginResponse = await Client.GetAsync(goToManageExternalLogin);

        return await ResponseAssert.IsHtmlDocumentAsync(manageExternalLoginResponse);
    }

    public async Task<LinkExternalLogin> CompleteExternalLoginAsReauthenticationAsync(string loginEmail)
    {
        var action = new Uri(Client.BaseAddress, _externalLoginForm.Action);
        var query = QueryHelpers.ParseQuery(action.Query)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        query["returnUrl"] = QueryHelpers.AddQueryString(
            query["returnUrl"],
            "reauthenticate",
            bool.TrueString);
        _externalLoginForm.Action = QueryHelpers.AddQueryString(action.GetLeftPart(UriPartial.Path), query);

        var document = await CompleteExternalLoginAsync(loginEmail);
        return new LinkExternalLogin(Client, document, Context);
    }
}
