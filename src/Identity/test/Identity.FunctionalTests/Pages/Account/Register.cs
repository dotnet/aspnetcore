// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account;

public class Register : DefaultUIPage
{
    private IHtmlFormElement _registerForm;
    private IHtmlFormElement _externalLoginForm;
    private readonly IHtmlElement _contosoButton;

    public Register(HttpClient client, IHtmlDocument register, DefaultUIContext context)
        : base(client, register, context)
    {
        _registerForm = HtmlAssert.HasForm("#registerForm", register);
        if (context.ContosoLoginEnabled)
        {
            _externalLoginForm = HtmlAssert.HasForm("#external-account", register);
            _contosoButton = HtmlAssert.HasElement("button[value=Contoso]", register);
        }
    }

    public async Task<Contoso.Login> ClickLoginWithContosoLinkAsync()
    {
        var externalFormResponse = await Client.SendAsync(_externalLoginForm, _contosoButton);
        var goToContosoLogin = ResponseAssert.IsRedirect(externalFormResponse);
        var contosoLoginResponse = await Client.GetAsync(goToContosoLogin);

        var contosoLogin = await ResponseAssert.IsHtmlDocumentAsync(contosoLoginResponse);

        return new Contoso.Login(Client, contosoLogin, Context);
    }

    public async Task<Index> SubmitRegisterFormForValidUserAsync(string userName, string password)
    {
        var registered = await Client.SendAsync(_registerForm, new Dictionary<string, string>()
        {
            ["Input_Email"] = userName,
            ["Input_Password"] = password,
            ["Input_ConfirmPassword"] = password
        });

        var registeredLocation = ResponseAssert.IsRedirect(registered);
        Assert.Equal(Index.Path, registeredLocation.ToString());
        var indexResponse = await Client.GetAsync(registeredLocation);
        var index = await ResponseAssert.IsHtmlDocumentAsync(indexResponse);

        return new Index(Client, index, Context.WithAuthenticatedUser());
    }

    public async Task<RegisterConfirmation> SubmitRegisterFormWithConfirmation(string userName, string password, bool hasRealEmail = false)
    {
        var registered = await Client.SendAsync(_registerForm, new Dictionary<string, string>()
        {
            ["Input_Email"] = userName,
            ["Input_Password"] = password,
            ["Input_ConfirmPassword"] = password
        });

        var registeredLocation = ResponseAssert.IsRedirect(registered);
        Assert.Equal(RegisterConfirmation.Path + "?email=" + userName + "&returnUrl=%2F", registeredLocation.ToString());
        var registerResponse = await Client.GetAsync(registeredLocation);
        var register = await ResponseAssert.IsHtmlDocumentAsync(registerResponse);

        return new RegisterConfirmation(Client, register, hasRealEmail ? Context.WithRealEmailSender() : Context);
    }
}
