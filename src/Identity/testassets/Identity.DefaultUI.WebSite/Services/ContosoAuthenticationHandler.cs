// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Identity.DefaultUI.WebSite;

public class ContosoAuthenticationHandler : AuthenticationHandler<ContosoAuthenticationOptions>
{
    public ContosoAuthenticationHandler(
        IOptionsMonitor<ContosoAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var uri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Options.RemoteLoginPath}";
        uri = QueryHelpers.AddQueryString(uri, new Dictionary<string, string>()
        {
            ["State"] = JsonConvert.SerializeObject(properties.Items),
            [Options.ReturnUrlQueryParameter] = properties.RedirectUri
        });
        Response.Redirect(uri);

        return Task.CompletedTask;
    }
}
