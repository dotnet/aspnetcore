// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Identity
{
    public class ClaimsIdentityOptions
    {
        /// <summary>
        ///     ClaimType used for the security stamp by default
        /// </summary>
        public static readonly string DefaultSecurityStampClaimType = "AspNet.Identity.SecurityStamp";

        public ClaimsIdentityOptions()
        {
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie;
            RoleClaimType = ClaimTypes.Role;
            SecurityStampClaimType = DefaultSecurityStampClaimType;
            UserIdClaimType = ClaimTypes.NameIdentifier;
            UserNameClaimType = ClaimTypes.Name;
        }

        public string AuthenticationType { get; set; }

        /// <summary>
        ///     Claim type used for role claims
        /// </summary>
        public string RoleClaimType { get; set; }

        /// <summary>
        ///     Claim type used for the user name
        /// </summary>
        public string UserNameClaimType { get; set; }

        /// <summary>
        ///     Claim type used for the user id
        /// </summary>
        public string UserIdClaimType { get; set; }

        /// <summary>
        ///     Claim type used for the user security stamp
        /// </summary>
        public string SecurityStampClaimType { get; set; }
    }
}