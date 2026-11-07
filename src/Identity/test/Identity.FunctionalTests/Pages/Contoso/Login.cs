// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Identity.FunctionalTests.Account;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Contoso;

public class Login : DefaultUIPage
{
    private readonly IHtmlFormElement _loginForm;

    public Login(HttpClient client, IHtmlDocument login, DefaultUIContext context)
        : base(client, login, context)
    {
        _loginForm = HtmlAssert.HasForm(login);
    }

    public async Task<ExternalLogin> SendNewUserNameAsync(string userName)
    {
        var externalLogin = await SendLoginForm(userName);

        return new ExternalLogin(Client, externalLogin, Context.WithSocialLoginProvider());
    }

    public async Task<Index> SendExistingUserNameAsync(string userName)
    {
        var externalLogin = await SendLoginForm(userName);

        return new Index(Client, externalLogin, Context.WithAuthenticatedUser());
    }

    // Used when the existing external-login account has two-factor authentication enabled:
    // the external sign-in must be challenged for a second factor instead of completing immediately.
    public async Task<Account.LoginWith2fa> SendExistingUserNameWith2FaAsync(string userName)
    {
        var contosoResponse = await Client.SendAsync(_loginForm, new Dictionary<string, string>
        {
            ["Input_Login"] = userName
        });

        var goToExternalLogin = ResponseAssert.IsRedirect(contosoResponse);
        var externalLogInResponse = await Client.GetAsync(goToExternalLogin);

        var goToLoginWith2fa = ResponseAssert.IsRedirect(externalLogInResponse);
        Assert.StartsWith(Account.LoginWith2fa.Path, goToLoginWith2fa.ToString());
        var loginWith2faResponse = await Client.GetAsync(goToLoginWith2fa);
        var loginWith2fa = await ResponseAssert.IsHtmlDocumentAsync(loginWith2faResponse);

        return new Account.LoginWith2fa(Client, loginWith2fa, Context);
    }

    private async Task<IHtmlDocument> SendLoginForm(string userName)
    {
        var contosoResponse = await Client.SendAsync(_loginForm, new Dictionary<string, string>
        {
            ["Input_Login"] = userName
        });

        var goToExternalLogin = ResponseAssert.IsRedirect(contosoResponse);
        var externalLogInResponse = await Client.GetAsync(goToExternalLogin);
        if (Context.ExistingUser)
        {
            var goToIndex = ResponseAssert.IsRedirect(externalLogInResponse);
            var indexResponse = await Client.GetAsync(goToIndex);
            return await ResponseAssert.IsHtmlDocumentAsync(indexResponse);
        }
        else
        {
            return await ResponseAssert.IsHtmlDocumentAsync(externalLogInResponse);
        }
    }
}
