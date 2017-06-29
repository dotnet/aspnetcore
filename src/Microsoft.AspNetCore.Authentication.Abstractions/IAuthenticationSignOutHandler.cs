// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication
{
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
        Task SignOutAsync(AuthenticationProperties properties);
    }

}
