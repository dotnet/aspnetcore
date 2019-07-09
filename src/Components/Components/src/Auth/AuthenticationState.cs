// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides information about the currently authenticated user, if any.
    /// </summary>
    public class AuthenticationState
    {
        /// <summary>
        /// Constructs an instance of <see cref="AuthenticationState"/>.
        /// </summary>
        /// <param name="user">A <see cref="ClaimsPrincipal"/> representing the user.</param>
        public AuthenticationState(ClaimsPrincipal user)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
        }

        /// <summary>
        /// Gets a <see cref="ClaimsPrincipal"/> that describes the current user.
        /// </summary>
        public ClaimsPrincipal User { get; }
    }
}
