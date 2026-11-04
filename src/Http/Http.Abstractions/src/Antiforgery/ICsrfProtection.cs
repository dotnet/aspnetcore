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
    /// A <see cref="ValueTask{TResult}"/> that resolves to a <see cref="CsrfProtectionResult"/> whose
    /// <see cref="CsrfProtectionResult.IsAllowed"/> is <see langword="true"/> when the request is
    /// same-origin, from a trusted origin, uses a safe HTTP method, or originates from a non-browser
    /// client; otherwise <see langword="false"/>.
    /// </returns>
    ValueTask<CsrfProtectionResult> ValidateAsync(HttpContext context);
}
