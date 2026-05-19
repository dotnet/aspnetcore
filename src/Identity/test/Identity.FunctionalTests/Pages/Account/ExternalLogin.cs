// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account;

public class ExternalLogin : DefaultUIPage
{
    private readonly IHtmlFormElement _emailForm;

    public ExternalLogin(
        HttpClient client,
        IHtmlDocument externalLogin,
        DefaultUIContext context)
        : base(client, externalLogin, context)
    {
        _emailForm = HtmlAssert.HasForm(Document);
        Title = HtmlAssert.HasElement("#external-login-title", Document);
        Description = HtmlAssert.HasElement("#external-login-description", Document);
    }

    public IHtmlElement Title { get; }
    public IHtmlElement Description { get; }

    public async Task<Index> SendEmailAsync(string email)
    {
        var response = await Client.SendAsync(_emailForm, new Dictionary<string, string>
        {
            ["Input_Email"] = email
        });
        var redirect = ResponseAssert.IsRedirect(response);
        var indexResponse = await Client.GetAsync(redirect);
        var index = await ResponseAssert.IsHtmlDocumentAsync(indexResponse);
        return new Index(Client, index, Context.WithAuthenticatedUser());
    }

    public async Task<RegisterConfirmation> SendEmailWithConfirmationAsync(string email, bool hasRealEmail)
    {
        var response = await Client.SendAsync(_emailForm, new Dictionary<string, string>
        {
            ["Input_Email"] = email
        });
        var redirect = ResponseAssert.IsRedirect(response);
        Assert.Equal(RegisterConfirmation.Path + "?email=" + email, redirect.ToString(), StringComparer.OrdinalIgnoreCase);

        var registerResponse = await Client.GetAsync(redirect);
        var register = await ResponseAssert.IsHtmlDocumentAsync(registerResponse);

        return new RegisterConfirmation(Client, register, hasRealEmail ? Context.WithRealEmailSender() : Context);
    }
}
