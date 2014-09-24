// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Identity
{
    public class ClaimsIdentityOptions
    {
        public static readonly string DefaultSecurityStampClaimType = "AspNet.Identity.SecurityStamp";
        public static readonly string DefaultAuthenticationType = typeof(ClaimsIdentityOptions).Namespace + ".Application";
        public static readonly string DefaultExternalLoginAuthenticationType = typeof(ClaimsIdentityOptions).Namespace + ".ExternalLogin";
        public static readonly string DefaultTwoFactorRememberMeAuthenticationType = typeof(ClaimsIdentityOptions).Namespace + ".TwoFactorRememberMe";
        public static readonly string DefaultTwoFactorUserIdAuthenticationType = typeof(ClaimsIdentityOptions).Namespace + ".TwoFactorUserId";

        public string AuthenticationType { get; set; } = DefaultAuthenticationType;

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