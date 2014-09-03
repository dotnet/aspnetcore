// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.OAuth
{
    public static class OAuthAuthenticationDefaults
    {
        public static readonly Func<OAuthGetUserInformationContext, Task> DefaultOnGetUserInformationAsync = context =>
        {
            // If the developer doesn't specify a user-info callback, just give them the tokens.
            var identity = new ClaimsIdentity(
                    context.Options.AuthenticationType,
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);

            identity.AddClaim(new Claim("access_token", context.AccessToken, ClaimValueTypes.String, context.Options.AuthenticationType));
            if (!string.IsNullOrEmpty(context.RefreshToken))
            {
                identity.AddClaim(new Claim("refresh_token", context.RefreshToken, ClaimValueTypes.String, context.Options.AuthenticationType));
            }
            if (!string.IsNullOrEmpty(context.TokenType))
            {
                identity.AddClaim(new Claim("token_type", context.TokenType, ClaimValueTypes.String, context.Options.AuthenticationType));
            }
            if (context.ExpiresIn.HasValue)
            {
                identity.AddClaim(new Claim("expires_in", context.ExpiresIn.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                    ClaimValueTypes.String, context.Options.AuthenticationType));
            }
            context.Identity = identity;
            return Task.FromResult(0);
        };
    }
}