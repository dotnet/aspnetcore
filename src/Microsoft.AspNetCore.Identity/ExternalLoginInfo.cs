// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Represents login information, source and externally source principal for a user record
    /// </summary>
    public class ExternalLoginInfo : UserLoginInfo
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExternalLoginInfo"/>
        /// </summary>
        /// <param name="externalPrincipal">The <see cref="ClaimsPrincipal"/> to associate with this login.</param>
        /// <param name="loginProvider">The provider associated with this login information.</param>
        /// <param name="providerKey">The unique identifier for this user provided by the login provider.</param>
        /// <param name="displayName">The display name for this user provided by the login provider.</param>
        public ExternalLoginInfo(ClaimsPrincipal externalPrincipal, string loginProvider, string providerKey, 
            string displayName) : base(loginProvider, providerKey, displayName)
        {
            ExternalPrincipal = externalPrincipal;
        }

        /// <summary>
        /// Gets or sets the <see cref="ClaimsPrincipal"/> associated with this login.
        /// </summary>
        /// <value>The <see cref="ClaimsPrincipal"/> associated with this login.</value>
        public ClaimsPrincipal ExternalPrincipal { get; set; }
    }
}