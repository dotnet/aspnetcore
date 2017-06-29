// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication
{
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
        Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties);
    }
}
