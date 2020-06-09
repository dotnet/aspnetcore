// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Authentication
{
    internal class IdentityServerJwtPolicySchemeForwardSelector
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
}
