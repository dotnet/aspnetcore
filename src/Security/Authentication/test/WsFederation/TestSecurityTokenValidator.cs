// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

internal class TestSecurityTokenValidator : ISecurityTokenValidator
{
    public bool CanValidateToken => true;

    public int MaximumTokenSizeInBytes { get; set; } = 1024 * 5;

    public bool CanReadToken(string securityToken)
    {
        return true;
    }

    public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
    {
        if (!string.IsNullOrEmpty(securityToken) && securityToken.Contains("ThisIsAValidToken"))
        {
            validatedToken = new TestSecurityToken();
            return new ClaimsPrincipal(new ClaimsIdentity("Test"));
        }

        throw new SecurityTokenException("The security token did not contain ThisIsAValidToken");
    }
}
