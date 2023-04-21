// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CustomAuthorizationFailureResponse.Authentication;

public class SampleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ClaimsPrincipal _id;

    public SampleAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
        _id = new ClaimsPrincipal(new ClaimsIdentity("Api"));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(_id, "Api")));
}
