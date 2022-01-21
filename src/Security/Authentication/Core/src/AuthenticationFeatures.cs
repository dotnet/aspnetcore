// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Keeps the User and AuthenticationResult consistent with each other
/// </summary>
internal sealed class AuthenticationFeatures : IAuthenticateResultFeature, IHttpAuthenticationFeature
{
    private ClaimsPrincipal? _user;
    private AuthenticateResult? _result;

    public AuthenticationFeatures(AuthenticateResult result)
    {
        AuthenticateResult = result;
    }

    public AuthenticateResult? AuthenticateResult
    {
        get => _result;
        set
        {
            _result = value;
            _user = _result?.Principal;
        }
    }

    public ClaimsPrincipal? User
    {
        get => _user;
        set
        {
            _user = value;
            _result = null;
        }
    }
}
