// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using AngleSharp.Dom.Html;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Account.Manage;

internal class EnableAuthenticator : DefaultUIPage
{
    public const string AuthenticatorKey = nameof(EnableAuthenticator) + "." + nameof(AuthenticatorKey);

    private readonly IHtmlElement _codeElement;
    private readonly IHtmlFormElement _sendCodeForm;

    public EnableAuthenticator(
        HttpClient client,
        IHtmlDocument enableAuthenticator,
        DefaultUIContext context)
        : base(client, enableAuthenticator, context)
    {
        Assert.True(Context.UserAuthenticated);
        _codeElement = HtmlAssert.HasElement("kbd", enableAuthenticator);
        _sendCodeForm = HtmlAssert.HasForm("#send-code", enableAuthenticator);
    }

    internal async Task<ShowRecoveryCodes> SendValidCodeAsync()
    {
        var authenticatorKey = _codeElement.TextContent.Replace(" ", "");
        Context.AuthenticatorKey = authenticatorKey;
        var verificationCode = ComputeCode(authenticatorKey);

        var sendCodeResponse = await Client.SendAsync(_sendCodeForm, new Dictionary<string, string>
        {
            ["Input_Code"] = verificationCode
        });

        var goToShowRecoveryCodes = ResponseAssert.IsRedirect(sendCodeResponse);
        var showRecoveryCodesResponse = await Client.GetAsync(goToShowRecoveryCodes);
        var showRecoveryCodes = await ResponseAssert.IsHtmlDocumentAsync(showRecoveryCodesResponse);

        return new ShowRecoveryCodes(Client, showRecoveryCodes, Context);
    }

    public static string ComputeCode(string key)
    {
        var keyBytes = Base32.FromBase32(key);
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestep = Convert.ToInt64(unixTimestamp / 30);
        var topt = Rfc6238AuthenticationService.ComputeTotp(keyBytes, (ulong)timestep, modifierBytes: null);
        return topt.ToString("D6", CultureInfo.InvariantCulture);
    }
}
