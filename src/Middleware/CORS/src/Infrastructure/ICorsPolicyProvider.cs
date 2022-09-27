// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// A type which can provide a <see cref="CorsPolicy"/> for a particular <see cref="HttpContext"/>.
/// </summary>
public interface ICorsPolicyProvider
{
    /// <summary>
    /// Gets a <see cref="CorsPolicy"/> from the given <paramref name="context"/>
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> associated with this call.</param>
    /// <param name="policyName">An optional policy name to look for.</param>
    /// <returns>A <see cref="CorsPolicy"/></returns>
    Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName);
}
