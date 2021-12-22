// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Used to determine if a handler supports SignOut.
/// </summary>
public interface IAuthenticationSignOutHandler : IAuthenticationHandler
{
    /// <summary>
    /// Signout behavior.
    /// </summary>
    /// <param name="properties">The <see cref="AuthenticationProperties"/> that contains the extra meta-data arriving with the authentication.</param>
    /// <returns>A task.</returns>
    Task SignOutAsync(AuthenticationProperties? properties);
}
