// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the link between a user and a role.
/// </summary>
/// <typeparam name="TKey">The type of the primary key used for users and roles.</typeparam>
public class IdentityUserRole<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the primary key of the user that is linked to a role.
    /// </summary>
    public virtual TKey UserId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the primary key of the role that is linked to the user.
    /// </summary>
    public virtual TKey RoleId { get; set; } = default!;
}
