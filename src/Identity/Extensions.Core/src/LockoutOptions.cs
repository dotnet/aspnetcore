// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Options for configuring user lockout.
/// </summary>
public class LockoutOptions
{
    /// <summary>
    /// Gets or sets a flag indicating whether a new user can be locked out. Defaults to true.
    /// </summary>
    /// <value>
    /// True if a newly created user can be locked out, otherwise false.
    /// </value>
    public bool AllowedForNewUsers { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of failed access attempts allowed before a user is locked out,
    /// assuming lock out is enabled. Defaults to 5.
    /// </summary>
    /// <value>
    /// The number of failed access attempts allowed before a user is locked out, if lockout is enabled.
    /// </value>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the <see cref="TimeSpan"/> a user is locked out for when a lockout occurs. Defaults to 5 minutes.
    /// </summary>
    /// <value>The <see cref="TimeSpan"/> a user is locked out for when a lockout occurs.</value>
    public TimeSpan DefaultLockoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);
}
