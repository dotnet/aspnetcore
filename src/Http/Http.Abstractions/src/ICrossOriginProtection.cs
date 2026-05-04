// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Provides cross-origin request validation based on Sec-Fetch-* and Origin headers.
/// This is a lightweight, synchronous CSRF protection mechanism that does not require
/// tokens or data protection.
/// </summary>
public interface ICrossOriginProtection
{
    /// <summary>
    /// Validates whether the request should be allowed based on its origin.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <returns>
    /// <see cref="CrossOriginAntiforgeryResult.Allowed"/> if the request is same-origin, from a trusted origin,
    /// uses a safe HTTP method, or originates from a non-browser client.
    /// <see cref="CrossOriginAntiforgeryResult.Denied"/> if the request is cross-origin and not trusted.
    /// </returns>
    CrossOriginAntiforgeryResult Validate(HttpContext httpContext);
}
