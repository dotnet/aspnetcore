// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides information about the currently authenticated user, if any.
    /// </summary>
    public interface IAuthenticationState
    {
        /// <summary>
        /// Gets a <see cref="ClaimsPrincipal"/> that describes the current user.
        /// </summary>
        ClaimsPrincipal User { get; }

        /// <summary>
        /// Gets a flag that indicates whether the authentication state is still being determined.
        /// </summary>
        public bool IsPending { get; }
    }
}
