// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class PersonalData : DefaultUIPage
{
    private readonly IHtmlAnchorElement _deleteLink;
    private readonly IHtmlFormElement _downloadForm;

    public PersonalData(HttpClient client, IHtmlDocument personalData, DefaultUIContext context)
        : base(client, personalData, context)
    {
        _deleteLink = HtmlAssert.HasLink("#delete", personalData);
        _downloadForm = HtmlAssert.HasForm("#download-data", personalData);
    }

    internal async Task<DeleteUser> ClickDeleteLinkAsync()
    {
        var goToDelete = await Client.GetAsync(_deleteLink.Href);
        var delete = await ResponseAssert.IsHtmlDocumentAsync(goToDelete);
        return new DeleteUser(Client, delete, Context.WithAnonymousUser());
    }

    internal async Task<HttpResponseMessage> SubmitDownloadForm()
    {
        return await Client.SendAsync(_downloadForm, new Dictionary<string, string>());
    }
}
