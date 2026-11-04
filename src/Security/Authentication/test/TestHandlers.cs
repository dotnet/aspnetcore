// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationSignInHandler
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    { }

    public int SignInCount { get; set; }
    public int SignOutCount { get; set; }
    public int ForbidCount { get; set; }
    public int ChallengeCount { get; set; }
    public int AuthenticateCount { get; set; }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        ChallengeCount++;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        ForbidCount++;
        return Task.CompletedTask;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AuthenticateCount++;
        var principal = new ClaimsPrincipal();
        var id = new ClaimsIdentity();
        id.AddClaim(new Claim(ClaimTypes.NameIdentifier, Scheme.Name, ClaimValueTypes.String, Scheme.Name));
        principal.AddIdentity(id);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), Scheme.Name)));
    }

    public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
    {
        SignInCount++;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(AuthenticationProperties properties)
    {
        SignOutCount++;
        return Task.CompletedTask;
    }
}

public class TestHandler : IAuthenticationSignInHandler
{
    public AuthenticationScheme Scheme { get; set; }
    public int SignInCount { get; set; }
    public int SignOutCount { get; set; }
    public int ForbidCount { get; set; }
    public int ChallengeCount { get; set; }
    public int AuthenticateCount { get; set; }

    public Task<AuthenticateResult> AuthenticateAsync()
    {
        AuthenticateCount++;
        var principal = new ClaimsPrincipal();
        var id = new ClaimsIdentity();
        id.AddClaim(new Claim(ClaimTypes.NameIdentifier, Scheme.Name, ClaimValueTypes.String, Scheme.Name));
        principal.AddIdentity(id);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), Scheme.Name)));
    }

    public Task ChallengeAsync(AuthenticationProperties properties)
    {
        ChallengeCount++;
        return Task.CompletedTask;
    }

    public Task ForbidAsync(AuthenticationProperties properties)
    {
        ForbidCount++;
        return Task.CompletedTask;
    }

    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        Scheme = scheme;
        return Task.CompletedTask;
    }

    public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
    {
        SignInCount++;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(AuthenticationProperties properties)
    {
        SignOutCount++;
        return Task.CompletedTask;
    }
}

public class TestHandler2 : TestHandler
{
}

public class TestHandler3 : TestHandler
{
}
