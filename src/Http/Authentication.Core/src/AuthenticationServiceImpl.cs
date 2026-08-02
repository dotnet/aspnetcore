// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class AuthenticationServiceImpl(
    IAuthenticationSchemeProvider schemes,
    IAuthenticationHandlerProvider handlers,
    IClaimsTransformation transform,
    IOptions<AuthenticationOptions> options,
    AuthenticationMetrics metrics)
    : AuthenticationService(schemes, handlers, transform, options)
{
    public override async Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
    {
        AuthenticateResult result;
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            result = await base.AuthenticateAsync(context, scheme);
        }
        catch (Exception ex)
        {
            metrics.AuthenticatedRequestCompleted(scheme, result: null, ex, startTimestamp, currentTimestamp: Stopwatch.GetTimestamp());
            throw;
        }

        metrics.AuthenticatedRequestCompleted(scheme, result, exception: result.Failure, startTimestamp, currentTimestamp: Stopwatch.GetTimestamp());
        return result;
    }

    public override async Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        try
        {
            await base.ChallengeAsync(context, scheme, properties);
        }
        catch (Exception ex)
        {
            metrics.ChallengeCompleted(scheme, ex);
            throw;
        }

        metrics.ChallengeCompleted(scheme, exception: null);
    }

    public override async Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        try
        {
            await base.ForbidAsync(context, scheme, properties);
        }
        catch (Exception ex)
        {
            metrics.ForbidCompleted(scheme, ex);
            throw;
        }

        metrics.ForbidCompleted(scheme, exception: null);
    }

    public override async Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        try
        {
            await base.SignInAsync(context, scheme, principal, properties);
        }
        catch (Exception ex)
        {
            metrics.SignInCompleted(scheme, ex);
            throw;
        }

        metrics.SignInCompleted(scheme, exception: null);
    }

    public override async Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        try
        {
            await base.SignOutAsync(context, scheme, properties);
        }
        catch (Exception ex)
        {
            metrics.SignOutCompleted(scheme, ex);
            throw;
        }

        metrics.SignOutCompleted(scheme, exception: null);
    }
}
