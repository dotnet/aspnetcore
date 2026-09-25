// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AngleSharp.Html.Dom;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

public class SetPassword : DefaultUIPage
{
    public const string Path = "/Identity/Account/Manage/SetPassword";
    public const string ReauthenticationCookieName = "Identity.Reauthentication";
    public const string ReauthenticationRefusal = "You must confirm your identity before setting a password.";

    private readonly IHtmlFormElement _setPasswordForm;

    public SetPassword(HttpClient client, IHtmlDocument setPassword, DefaultUIContext context)
        : base(client, setPassword, context)
    {
        _setPasswordForm = HtmlAssert.HasForm("#set-password-form", setPassword);
    }

    public async Task<SetPassword> SetPasswordAsync(string newPassword)
    {
        var response = await Client.SendAsync(_setPasswordForm, new Dictionary<string, string>
        {
            ["Input_NewPassword"] = newPassword,
            ["Input_ConfirmPassword"] = newPassword
        });
        Assert.Equal(Path, ResponseAssert.IsRedirect(response).OriginalString);

        return this;
    }

    public async Task<SetPassword> ReauthenticateAsync(string login)
    {
        var form = HtmlAssert.HasForm("#reauthenticate-form", Document);
        var button = HtmlAssert.HasElement("button[value=Contoso]", form);
        var challenge = await Client.SendAsync(form, button);
        var markerCookies = challenge.Headers.Where(header => header.Key == "Set-Cookie")
            .SelectMany(header => header.Value)
            .Where(value => value.StartsWith(ReauthenticationCookieName + "=", StringComparison.Ordinal));
        // A challenge may delete an earlier marker, but must not issue one.
        Assert.All(markerCookies, value => Assert.StartsWith(ReauthenticationCookieName + "=;", value));
        var externalLoginResponse = await Client.GetAsync(ResponseAssert.IsRedirect(challenge));
        var externalLoginDocument = await ResponseAssert.IsHtmlDocumentAsync(externalLoginResponse);
        var externalLoginForm = HtmlAssert.HasForm("#external-login", externalLoginDocument);
        var externalLogin = await Client.SendAsync(externalLoginForm, new Dictionary<string, string>
        {
            ["Input_Login"] = login
        });

        var callback = await Client.GetAsync(ResponseAssert.IsRedirect(externalLogin));
        Assert.Equal(Path, ResponseAssert.IsRedirect(callback).OriginalString);
        var response = await Client.GetAsync(callback.Headers.Location);
        var document = await ResponseAssert.IsHtmlDocumentAsync(response);
        return new SetPassword(Client, document, Context);
    }

    public Task<HttpResponseMessage> PostReauthenticationAsync(string provider)
    {
        var values = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = GetAntiforgeryToken()
        };
        if (provider is not null)
        {
            values["provider"] = provider;
        }

        return Client.PostAsync($"{Path}?handler=Reauthenticate", new FormUrlEncodedContent(values));
    }

    public Task<HttpResponseMessage> PostPasswordAsync(string newPassword, string confirmPassword = null)
    {
        return Client.PostAsync(Path, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = GetAntiforgeryToken(),
            ["Input.NewPassword"] = newPassword,
            ["Input.ConfirmPassword"] = confirmPassword ?? newPassword
        }));
    }

    private string GetAntiforgeryToken() =>
        Assert.IsAssignableFrom<IHtmlInputElement>(_setPasswordForm["__RequestVerificationToken"]).Value;
}
