// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.CrossOrigin;

/// <summary>
/// Defines a contract for validating cross-origin antiforgery tokens in HTTP requests.
/// </summary>
/// <remarks>Implementations of this interface are responsible for verifying that incoming requests from different
/// origins include valid antiforgery tokens, helping to protect against cross-site request forgery (CSRF) attacks. This
/// interface is typically used in scenarios where cross-origin requests require additional validation beyond standard
/// antiforgery mechanisms.</remarks>
public interface ICrossOriginAntiforgery
{
    /// <summary>
    /// Validates the incoming HTTP request against cross-origin antiforgery requirements using the specified options.
    /// </summary>
    /// <param name="context">The HTTP context representing the current request to validate. Cannot be null.</param>
    /// <param name="options">The options that configure cross-origin antiforgery validation behavior. Cannot be null.</param>
    /// <returns>A CrossOriginValidationResult indicating whether the request passed validation and, if not, the reason for
    /// failure.</returns>
    public CrossOriginValidationResult Validate(HttpContext context);
}
