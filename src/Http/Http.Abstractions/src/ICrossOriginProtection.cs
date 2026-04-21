// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Provides cross-origin request protection based on Fetch Metadata headers
/// (Sec-Fetch-Site) and Origin header validation. This is a lightweight
/// defense against CSRF attacks that does not require tokens or DataProtection.
/// </summary>
public interface ICrossOriginProtection
{
    /// <summary>
    /// Validates whether the current HTTP request should be allowed based on
    /// cross-origin protection rules.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>
    /// <see cref="CrossOriginValidationResult.Allowed"/> if the request passes cross-origin validation;
    /// <see cref="CrossOriginValidationResult.Denied"/> if the request is a suspected cross-origin attack;
    /// <see cref="CrossOriginValidationResult.Disabled"/> if cross-origin protection is not enabled.
    /// </returns>
    CrossOriginValidationResult Validate(HttpContext context);
}
