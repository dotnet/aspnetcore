// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Identity
{
    public class ClaimTypeOptions
    {
        /// <summary>
        ///     ClaimType used for the security stamp by default
        /// </summary>
        public static readonly string DefaultSecurityStampClaimType = "AspNet.Identity.SecurityStamp";

        public ClaimTypeOptions()
        {
            Role = ClaimTypes.Role;
            SecurityStamp = DefaultSecurityStampClaimType;
            UserId = ClaimTypes.NameIdentifier;
            UserName = ClaimTypes.Name;
        }

        /// <summary>
        ///     Claim type used for role claims
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        ///     Claim type used for the user name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///     Claim type used for the user id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        ///     Claim type used for the user security stamp
        /// </summary>
        public string SecurityStamp { get; set; }
    }
}