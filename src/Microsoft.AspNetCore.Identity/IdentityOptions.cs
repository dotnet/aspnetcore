// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Represents all the options you can use to configure the identity system.
    /// </summary>
    public class IdentityOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="ClaimsIdentityOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="ClaimsIdentityOptions"/> for the identity system.
        /// </value>
        public ClaimsIdentityOptions ClaimsIdentity { get; set; } = new ClaimsIdentityOptions();

        /// <summary>
        /// Gets or sets the <see cref="UserOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="UserOptions"/> for the identity system.
        /// </value>
        public UserOptions User { get; set; } = new UserOptions();

        /// <summary>
        /// Gets or sets the <see cref="PasswordOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="PasswordOptions"/> for the identity system.
        /// </value>
        public PasswordOptions Password { get; set; } = new PasswordOptions();

        /// <summary>
        /// Gets or sets the <see cref="LockoutOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="LockoutOptions"/> for the identity system.
        /// </value>
        public LockoutOptions Lockout { get; set; } = new LockoutOptions();

        /// <summary>
        /// Gets or sets the <see cref="SignInOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="SignInOptions"/> for the identity system.
        /// </value>
        public SignInOptions SignIn { get; set; } = new SignInOptions();

        /// <summary>
        /// Gets or sets the <see cref="IdentityCookieOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="IdentityCookieOptions"/> for the identity system.
        /// </value>
        public IdentityCookieOptions Cookies { get; set; } = new IdentityCookieOptions();

        /// <summary>
        /// Gets or sets the <see cref="TokenOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="TokenOptions"/> for the identity system.
        /// </value>
        public TokenOptions Tokens { get; set; } = new TokenOptions();

        /// <summary>
        /// Gets or sets the <see cref="TimeSpan"/> after which security stamps are re-validated.
        /// </summary>
        /// <value>
        /// The <see cref="TimeSpan"/> after which security stamps are re-validated.
        /// </value>
        public TimeSpan SecurityStampValidationInterval { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Invoked when the default security stamp validator replaces the user's ClaimsPrincipal in the cookie.
        /// </summary>
        public Func<SecurityStampRefreshingPrincipalContext, Task> OnSecurityStampRefreshingPrincipal { get; set; }
    }
}