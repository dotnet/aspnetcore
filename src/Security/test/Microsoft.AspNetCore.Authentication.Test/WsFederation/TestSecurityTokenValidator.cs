// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
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
}