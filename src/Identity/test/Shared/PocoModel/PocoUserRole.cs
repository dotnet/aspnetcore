// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Microsoft.AspNetCore.Identity.Test;

/// <summary>
///     EntityType that represents a user belonging to a role
/// </summary>
public class PocoUserRole : PocoUserRole<string> { }

/// <summary>
///     EntityType that represents a user belonging to a role
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class PocoUserRole<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    ///     UserId for the user that is in the role
    /// </summary>
    public virtual TKey UserId { get; set; }

    /// <summary>
    ///     RoleId for the role
    /// </summary>
    public virtual TKey RoleId { get; set; }
}
