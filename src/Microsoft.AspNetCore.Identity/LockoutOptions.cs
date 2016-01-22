// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Options for configuring user lockout.
    /// </summary>
    public class LockoutOptions
    {
        /// <value>
        /// True if a newly created user can be locked out, otherwise false.
        /// </value>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool AllowedForNewUsers { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of failed access attempts allowed before a user is locked out,
        /// assuming lock out is enabled.
        /// </summary>
        /// <value>
        /// The number of failed access attempts allowed before a user is locked out, if lockout is enabled.
        /// </value>
        /// <remarks>Defaults to 5 failed attempts before an account is locked out.</remarks>
        public int MaxFailedAccessAttempts { get; set; } = 5;

        /// <summary>
        /// Gets or sets the <see cref="TimeSpan"/> a user is locked out for when a lockout occurs.
        /// </summary>
        /// <value>The <see cref="TimeSpan"/> a user is locked out for when a lockout occurs.</value>
        /// <remarks>Defaults to 5 minutes.</remarks>
        public TimeSpan DefaultLockoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);
    }
}