// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Identity
{
    public class ClaimsIdentityOptions
    {
        public static readonly string DefaultSecurityStampClaimType = "AspNet.Identity.SecurityStamp";

        /// <summary>
        ///     Claim type used for role claims
        /// </summary>
        public string RoleClaimType { get; set; } = ClaimTypes.Role;

        /// <summary>
        ///     Claim type used for the user name
        /// </summary>
        public string UserNameClaimType { get; set; } = ClaimTypes.Name;

        /// <summary>
        ///     Claim type used for the user id
        /// </summary>
        public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

        /// <summary>
        ///     Claim type used for the user security stamp
        /// </summary>
        public string SecurityStampClaimType { get; set; } = DefaultSecurityStampClaimType;
    }
}