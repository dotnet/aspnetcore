// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class DeleteUser : DefaultUIPage
{
    private readonly IHtmlFormElement _deleteForm;

    public DeleteUser(HttpClient client, IHtmlDocument deleteUser, DefaultUIContext context)
        : base(client, deleteUser, context)
    {
        _deleteForm = HtmlAssert.HasForm("#delete-user", deleteUser);
    }

    public async Task<FunctionalTests.Index> Delete(string password)
    {
        var loggedIn = await SendDeleteForm(password);

        var deleteLocation = ResponseAssert.IsRedirect(loggedIn);
        Assert.Equal(Index.Path, deleteLocation.ToString());
        var indexResponse = await Client.GetAsync(deleteLocation);
        var index = await ResponseAssert.IsHtmlDocumentAsync(indexResponse);
        return new FunctionalTests.Index(Client, index, Context);
    }

    private async Task<HttpResponseMessage> SendDeleteForm(string password)
    {
        return await Client.SendAsync(_deleteForm, new Dictionary<string, string>()
        {
            ["Input_Password"] = password
        });
    }
}
