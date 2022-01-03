// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Allow custom handling of authorization and handling of the authorization response.
/// </summary>
public interface IAuthorizationMiddlewareResultHandler
{
    /// <summary>
    /// Evaluates the authorization requirement and processes the authorization result.
    /// </summary>
    /// <param name="next">
    /// The next middleware in the application pipeline. Implementations may not invoke this if the authorization did not succeed.
    /// </param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/> for the resource.</param>
    /// <param name="authorizeResult">The result of authorization.</param>
    Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult);
}
