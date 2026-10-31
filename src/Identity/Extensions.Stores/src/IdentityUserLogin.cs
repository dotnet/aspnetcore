// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a login and its associated provider for a user.
/// </summary>
/// <typeparam name="TKey">The type of the primary key of the user associated with this login.</typeparam>
public class IdentityUserLogin<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the login provider for the login (e.g. facebook, google)
    /// </summary>
    public virtual string LoginProvider { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unique provider identifier for this login.
    /// </summary>
    public virtual string ProviderKey { get; set; } = default!;

    /// <summary>
    /// Gets or sets the friendly name used in a UI for this login.
    /// </summary>
    public virtual string? ProviderDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the primary key of the user associated with this login.
    /// </summary>
    public virtual TKey UserId { get; set; } = default!;
}
