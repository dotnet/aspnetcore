// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed class AuthenticationHandler : IAuthenticationHandler
{
    private RequestContext? _requestContext;
    private AuthenticationScheme? _scheme;

    public Task<AuthenticateResult> AuthenticateAsync()
    {
        var identity = _requestContext!.User?.Identity;
        if (identity != null && identity.IsAuthenticated)
        {
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(_requestContext.User!, properties: null, authenticationScheme: _scheme!.Name)));
        }
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    public Task ChallengeAsync(AuthenticationProperties? properties)
    {
        _requestContext!.Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    public Task ForbidAsync(AuthenticationProperties? properties)
    {
        _requestContext!.Response.StatusCode = 403;
        return Task.CompletedTask;
    }

    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
    {
        _scheme = scheme;
        _requestContext = context.Features.Get<RequestContext>();

        if (_requestContext == null)
        {
            throw new InvalidOperationException("No RequestContext found.");
        }

        return Task.CompletedTask;
    }
}
