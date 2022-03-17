// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization.Test.TestObjects;

public class TestAuthenticationService : IAuthenticationService
{
    public bool ChallengeCalled => ChallengeCount > 0;
    public bool ForbidCalled => ForbidCount > 0;
    public bool AuthenticateCalled => AuthenticateCount > 0;

    public int ChallengeCount { get; private set; }
    public int ForbidCount { get; private set; }
    public int AuthenticateCount { get; private set; }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
    {
        AuthenticateCount++;

        var identity = context.User.Identities.SingleOrDefault(i => i.AuthenticationType == scheme);
        if (identity != null)
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), scheme)));
        }

        return Task.FromResult(AuthenticateResult.Fail("Denied"));
    }

    public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
    {
        ChallengeCount++;
        return Task.CompletedTask;
    }

    public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
    {
        ForbidCount++;
        return Task.CompletedTask;
    }

    public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
    {
        throw new NotImplementedException();
    }

    public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
    {
        throw new NotImplementedException();
    }
}
