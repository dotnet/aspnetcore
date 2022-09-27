// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class Email : DefaultUIPage
{
    private readonly IHtmlElement _emailInput;
    private readonly IHtmlElement _newEmailInput;
    private readonly IHtmlElement _confirmEmailButton;
    private readonly IHtmlFormElement _changeEmailForm;
    private readonly IHtmlElement _changeEmailButton;

    public static readonly string Path = "/";

    public Email(HttpClient client, IHtmlDocument manage, DefaultUIContext context)
        : base(client, manage, context)
    {
        Assert.True(Context.UserAuthenticated);

        _changeEmailForm = HtmlAssert.HasForm("#email-form", manage);
        _emailInput = HtmlAssert.HasElement("#Email", manage);
        _newEmailInput = HtmlAssert.HasElement("#Input_NewEmail", manage);
        _changeEmailButton = HtmlAssert.HasElement("#change-email-button", manage);
        if (!Context.EmailConfirmed)
        {
            _confirmEmailButton = HtmlAssert.HasElement("button#email-verification", manage);
        }
    }

    internal async Task<Email> SendConfirmationEmailAsync()
    {
        Assert.False(Context.EmailConfirmed);

        var response = await Client.SendAsync(_changeEmailForm, _confirmEmailButton);
        var goToManage = ResponseAssert.IsRedirect(response);
        var manageResponse = await Client.GetAsync(goToManage);
        var manage = await ResponseAssert.IsHtmlDocumentAsync(manageResponse);

        return new Email(Client, manage, Context);
    }

    internal async Task<Email> SendUpdateEmailAsync(string newEmail)
    {
        var response = await Client.SendAsync(_changeEmailForm, _changeEmailButton, new Dictionary<string, string>
        {
            ["Input_NewEmail"] = newEmail
        });
        var goToManage = ResponseAssert.IsRedirect(response);
        var manageResponse = await Client.GetAsync(goToManage);
        var manage = await ResponseAssert.IsHtmlDocumentAsync(manageResponse);

        return new Email(Client, manage, Context);
    }

    internal object GetEmail() => _emailInput.GetAttribute("value");

    internal object GetNewEmail() => _newEmailInput.GetAttribute("value");
}
