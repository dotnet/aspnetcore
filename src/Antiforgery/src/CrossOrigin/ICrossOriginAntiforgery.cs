// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.CrossOrigin;

/// <summary>
/// Provides cross-origin request validation using Fetch Metadata headers.
/// </summary>
public interface ICrossOriginAntiforgery
{
    /// <summary>
    /// Validates the incoming HTTP request against cross-origin antiforgery requirements.
    /// </summary>
    /// <param name="context">The HTTP context representing the current request to validate.</param>
    /// <returns>A <see cref="CrossOriginValidationResult"/> indicating whether the request is allowed, denied, or unknown.</returns>
    CrossOriginValidationResult Validate(HttpContext context);
}
