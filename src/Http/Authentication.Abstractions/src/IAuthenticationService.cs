// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used to provide authentication.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate for the specified authentication scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <returns>The result.</returns>
    Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme);

    /// <summary>
    /// Challenge the specified authentication scheme.
    /// An authentication challenge can be issued when an unauthenticated user requests an endpoint that requires authentication.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
    /// <returns>A task.</returns>
    Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties);

    /// <summary>
    /// Forbids the specified authentication scheme.
    /// Forbid is used when an authenticated user attempts to access a resource they are not permitted to access.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
    /// <returns>A task.</returns>
    Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties);

    /// <summary>
    /// Sign a principal in for the specified authentication scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to sign in.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
    /// <returns>A task.</returns>
    Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties);

    /// <summary>
    /// Sign out the specified authentication scheme.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The name of the authentication scheme.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/>.</param>
    /// <returns>A task.</returns>
    Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties);
}
