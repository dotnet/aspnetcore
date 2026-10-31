// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used by the <see cref="IAuthenticationService"/> for claims transformation.
/// </summary>
public interface IClaimsTransformation
{
    /// <summary>
    /// Provides a central transformation point to change the specified principal.
    /// Note: this will be run on each AuthenticateAsync call, so its safer to
    /// return a new ClaimsPrincipal if your transformation is not idempotent.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to transform.</param>
    /// <returns>The transformed principal.</returns>
    Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal);
}
