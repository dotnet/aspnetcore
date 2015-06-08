// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Options for configuring user lockout.
    /// </summary>
    public class LockoutOptions
    {
        /// <summary>
        /// Gets or sets a flag indicating whether users are locked out upon creation.
        /// </summary>
        /// <value>
        /// True if a newly created user is locked out, otherwise false.
        /// </value>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool EnabledByDefault { get; set; } = false;

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