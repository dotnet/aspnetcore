// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;
using Microsoft.AspNetCore.Identity.FunctionalTests.Account;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public class Index : DefaultUIPage
{
    private readonly IHtmlAnchorElement _registerLink;
    private readonly IHtmlAnchorElement _loginLink;
    private readonly IHtmlAnchorElement _manageLink;
    public static readonly string Path = "/";

    public Index(
        HttpClient client,
        IHtmlDocument index,
        DefaultUIContext context)
        : base(client, index, context)
    {
        if (!Context.UserAuthenticated)
        {
            _registerLink = HtmlAssert.HasLink("#register", Document);
            _loginLink = HtmlAssert.HasLink("#login", Document);
        }
        else
        {
            _manageLink = HtmlAssert.HasLink("#manage", Document);
        }
    }

    public static async Task<Index> CreateAsync(HttpClient client, DefaultUIContext context = null)
    {
        var goToIndex = await client.GetAsync("/");
        var index = await ResponseAssert.IsHtmlDocumentAsync(goToIndex);

        return new Index(client, index, context ?? new DefaultUIContext());
    }

    public async Task<Register> ClickRegisterLinkAsync()
    {
        Assert.False(Context.UserAuthenticated);

        var goToRegister = await Client.GetAsync(_registerLink.Href);
        var register = await ResponseAssert.IsHtmlDocumentAsync(goToRegister);

        return new Register(Client, register, Context);
    }

    public async Task<Login> ClickLoginLinkAsync()
    {
        Assert.False(Context.UserAuthenticated);

        var goToLogin = await Client.GetAsync(_loginLink.Href);
        var login = await ResponseAssert.IsHtmlDocumentAsync(goToLogin);

        return new Login(Client, login, Context);
    }

    internal async Task<Account.Manage.Index> ClickManageLinkAsync()
    {
        Assert.True(Context.UserAuthenticated);

        var goToManage = await Client.GetAsync(_manageLink.Href);
        var manage = await ResponseAssert.IsHtmlDocumentAsync(goToManage);

        return new Account.Manage.Index(Client, manage, Context);
    }

    internal async Task<Account.Manage.Index> ClickManageLinkWithExternalLoginAsync()
    {
        Assert.True(Context.UserAuthenticated);

        var goToManage = await Client.GetAsync(_manageLink.Href);
        var manage = await ResponseAssert.IsHtmlDocumentAsync(goToManage);

        return new Account.Manage.Index(Client, manage, Context
            .WithSocialLoginEnabled()
            .WithSocialLoginProvider());
    }
}
