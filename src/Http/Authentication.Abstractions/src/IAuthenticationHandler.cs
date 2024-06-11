// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Created per request to handle authentication for a particular scheme.
/// </summary>
public interface IAuthenticationHandler
{
    /// <summary>
    /// Initialize the authentication handler. The handler should initialize anything it needs from the request and scheme as part of this method.
    /// </summary>
    /// <param name="scheme">The <see cref="AuthenticationScheme"/> scheme.</param>
    /// <param name="context">The <see cref="HttpContext"/> context.</param>
    Task InitializeAsync(AuthenticationScheme scheme, HttpContext context);

    /// <summary>
    /// Authenticate the request associated with the <see cref="HttpContext"/> passed to the InitializeAsync method.
    /// </summary>
    /// <returns>The <see cref="AuthenticateResult"/> result.</returns>
    Task<AuthenticateResult> AuthenticateAsync();

    /// <summary>
    /// Challenge the request associated with the <see cref="HttpContext"/> passed to the InitializeAsync method.
    /// </summary>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> that contains the extra meta-data arriving with the authentication.</param>
    Task ChallengeAsync(AuthenticationProperties? properties);

    /// <summary>
    /// Forbid the request associated with the <see cref="HttpContext"/> passed to the InitializeAsync method.
    /// </summary>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> that contains the extra meta-data arriving with the authentication.</param>
    Task ForbidAsync(AuthenticationProperties? properties);
}
