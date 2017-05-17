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
        /// Gets or sets the ClaimType used for a Role claim.
        /// </summary>
        /// <remarks>
        /// This defaults to <see cref="ClaimTypes.Role"/>.
        /// </remarks>
        public string RoleClaimType { get; set; } = ClaimTypes.Role;

        /// <summary>
        /// Gets or sets the ClaimType used for the user name claim.
        /// </summary>
        /// <remarks>
        /// This defaults to <see cref="ClaimTypes.Name"/>.
        /// </remarks>
        public string UserNameClaimType { get; set; } = ClaimTypes.Name;

        /// <summary>
        /// Gets or sets the ClaimType used for the user identifier claim.
        /// </summary>
        /// <remarks>
        /// This defaults to <see cref="ClaimTypes.NameIdentifier"/>.
        /// </remarks>
        public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

        /// <summary>
        /// Gets or sets the ClaimType used for the security stamp claim.
        /// </summary>
        /// <remarks>
        /// This defaults to "AspNet.Identity.SecurityStamp".
        /// </remarks>
        public string SecurityStampClaimType { get; set; } = "AspNet.Identity.SecurityStamp";
    }
}