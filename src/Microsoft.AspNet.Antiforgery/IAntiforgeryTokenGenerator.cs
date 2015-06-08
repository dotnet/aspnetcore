// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Antiforgery
{
    // Provides configuration information about the anti-forgery system.
    public interface IAntiforgeryTokenGenerator
    {
        // Generates a new random cookie token.
        AntiforgeryToken GenerateCookieToken();

        // Given a cookie token, generates a corresponding form token.
        // The incoming cookie token must be valid.
        AntiforgeryToken GenerateFormToken(
            HttpContext httpContext,
            ClaimsIdentity identity,
            AntiforgeryToken cookieToken);
    }
}