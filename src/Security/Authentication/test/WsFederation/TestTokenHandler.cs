// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

internal class TestSecurityTokenHandler : TokenHandler
{
    public override SecurityToken ReadToken(string token)
    {
        return new TestSecurityToken();
    }

    public override Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        if (!string.IsNullOrEmpty(token) && token.Contains("ThisIsAValidToken"))
        {
            return Task.FromResult(new TokenValidationResult
            {
                ClaimsIdentity = new ClaimsIdentity("Test"),
                IsValid = true,
                SecurityToken = new TestSecurityToken()
            });
        }

        throw new SecurityTokenException("The security token did not contain ThisIsAValidToken");
    }
}
