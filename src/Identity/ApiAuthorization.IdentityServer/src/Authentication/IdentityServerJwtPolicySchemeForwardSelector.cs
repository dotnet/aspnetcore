// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Authentication;

internal sealed class IdentityServerJwtPolicySchemeForwardSelector
{
    private readonly PathString _identityPath;
    private readonly string _IdentityServerJwtScheme;

    public IdentityServerJwtPolicySchemeForwardSelector(
        string identityPath,
        string IdentityServerJwtScheme)
    {
        _identityPath = identityPath;
        _IdentityServerJwtScheme = IdentityServerJwtScheme;
    }

    public string SelectScheme(HttpContext ctx)
    {
        if (ctx.Request.Path.StartsWithSegments(_identityPath, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityConstants.ApplicationScheme;
        }

        return _IdentityServerJwtScheme;
    }
}
