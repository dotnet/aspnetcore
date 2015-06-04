// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    // Provides an abstraction around something that can validate anti-XSRF tokens
    internal interface IAntiForgeryTokenValidator
    {
        // Determines whether an existing cookie token is valid (well-formed).
        // If it is not, the caller must call GenerateCookieToken() before calling GenerateFormToken().
        bool IsCookieTokenValid(AntiForgeryToken cookieToken);

        // Validates a (cookie, form) token pair.
        void ValidateTokens(
            HttpContext httpContext,
            ClaimsIdentity identity,
            AntiForgeryToken cookieToken,
            AntiForgeryToken formToken);
    }
}