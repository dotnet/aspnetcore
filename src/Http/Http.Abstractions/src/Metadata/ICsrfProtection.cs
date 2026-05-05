// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Provides cross-origin request protection based on Fetch Metadata headers
/// (Sec-Fetch-Site) and Origin header validation. This is a lightweight
/// defense against CSRF attacks that does not require tokens or DataProtection.
/// </summary>
public interface ICsrfProtection
{
    /// <summary>
    /// Validates whether the request should be allowed based on its origin.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <returns>
    /// <see cref="CsrfProtectionResult.Allowed"/> if the request is same-origin, from a trusted origin,
    /// uses a safe HTTP method, or originates from a non-browser client.
    /// <see cref="CsrfProtectionResult.Denied"/> if the request is cross-origin and not trusted.
    /// </returns>
    CsrfProtectionResult Validate(HttpContext context);
}
