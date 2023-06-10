// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Options for <see cref="ISecurityStampValidator"/>.
/// </summary>
public class SecurityStampValidatorOptions
{
    /// <summary>
    /// Gets or sets the <see cref="TimeSpan"/> after which security stamps are re-validated. Defaults to 30 minutes.
    /// </summary>
    /// <value>
    /// The <see cref="TimeSpan"/> after which security stamps are re-validated.
    /// </value>
    public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Invoked when the default security stamp validator replaces the user's ClaimsPrincipal in the cookie.
    /// </summary>
    public Func<SecurityStampRefreshingPrincipalContext, Task>? OnRefreshingPrincipal { get; set; }

    /// <summary>
    /// Gives control over the timestamps for testing purposes.
    /// </summary>
    public TimeProvider? TimeProvider { get; set; }
}
