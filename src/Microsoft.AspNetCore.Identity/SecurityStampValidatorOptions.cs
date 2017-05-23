// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Options for <see cref="ISecurityStampValidator"/>.
    /// </summary>
    public class SecurityStampValidatorOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="TimeSpan"/> after which security stamps are re-validated.
        /// </summary>
        /// <value>
        /// The <see cref="TimeSpan"/> after which security stamps are re-validated.
        /// </value>
        public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Invoked when the default security stamp validator replaces the user's ClaimsPrincipal in the cookie.
        /// </summary>
        public Func<SecurityStampRefreshingPrincipalContext, Task> OnRefreshingPrincipal { get; set; }
    }
}