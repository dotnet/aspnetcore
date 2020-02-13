// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents options to use when configuring the <see cref="System.Security.Claims.ClaimsPrincipal"/> for a user.
    /// </summary>
    public class RemoteAuthenticationUserOptions
    {
        /// <summary>
        /// Gets or sets the claim type to use for the user name.
        /// </summary>
        public string NameClaim { get; set; } = "name";

        /// <summary>
        /// Gets or sets the claim type to use for the user roles.
        /// </summary>
        public string RoleClaim { get; set; }

        /// <summary>
        /// Gets or sets the claim type to use for the user scopes.
        /// </summary>
        public string ScopeClaim { get; set; }

        /// <summary>
        /// Gets or sets the value to use for the <see cref="System.Security.Claims.ClaimsIdentity.AuthenticationType"/>.
        /// </summary>
        public string AuthenticationType { get; set; }
    }
}
