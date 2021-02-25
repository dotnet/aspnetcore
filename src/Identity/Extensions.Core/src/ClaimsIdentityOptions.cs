// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Options used to configure the claim types used for well known claims.
    /// </summary>
    public class ClaimsIdentityOptions
    {
        /// <summary>
        /// Gets or sets the ClaimType used for a Role claim. Defaults to <see cref="ClaimTypes.Role"/>.
        /// </summary>
        public string RoleClaimType { get; set; } = ClaimTypes.Role;

        /// <summary>
        /// Gets or sets the ClaimType used for the user name claim. Defaults to <see cref="ClaimTypes.Name"/>.
        /// </summary>
        public string UserNameClaimType { get; set; } = ClaimTypes.Name;

        /// <summary>
        /// Gets or sets the ClaimType used for the user identifier claim. Defaults to <see cref="ClaimTypes.NameIdentifier"/>.
        /// </summary>
        public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

        /// <summary>
        /// Gets or sets the ClaimType used for the user email claim. Defaults to <see cref="ClaimTypes.Email"/>.
        /// </summary>
        public string EmailClaimType { get; set; } = ClaimTypes.Email;

        /// <summary>
        /// Gets or sets the ClaimType used for the security stamp claim. Defaults to "AspNet.Identity.SecurityStamp".
        /// </summary>
        public string SecurityStampClaimType { get; set; } = "AspNet.Identity.SecurityStamp";
    }
}
