// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used to determine if a handler supports SignIn.
/// </summary>
public interface IAuthenticationSignInHandler : IAuthenticationSignOutHandler
{
    /// <summary>
    /// Handle sign in.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> user.</param>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> that contains the extra meta-data arriving with the authentication.</param>
    /// <returns>A task.</returns>
    Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties);
}
