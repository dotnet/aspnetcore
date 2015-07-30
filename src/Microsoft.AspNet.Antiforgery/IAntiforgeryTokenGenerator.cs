// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Antiforgery
{
    /// <summary>
    /// Generates and validates antiforgery tokens.
    /// </summary>
    public interface IAntiforgeryTokenGenerator
    {
        // Generates a new random cookie token.
        AntiforgeryToken GenerateCookieToken();

        // Given a cookie token, generates a corresponding form token.
        // The incoming cookie token must be valid.
        AntiforgeryToken GenerateFormToken(
            [NotNull] HttpContext httpContext,
            AntiforgeryToken cookieToken);

        // Determines whether an existing cookie token is valid (well-formed).
        // If it is not, the caller must call GenerateCookieToken() before calling GenerateFormToken().
        bool IsCookieTokenValid(AntiforgeryToken cookieToken);

        // Validates a (cookie, form) token pair.
        void ValidateTokens(
            [NotNull] HttpContext httpContext,
            [NotNull] AntiforgeryToken cookieToken,
            [NotNull] AntiforgeryToken formToken);
    }
}