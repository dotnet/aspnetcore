// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

/// <summary>
///     EntityType that represents one specific role claim
/// </summary>
public class PocoRoleClaim : PocoRoleClaim<string> { }

/// <summary>
///     EntityType that represents one specific role claim
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class PocoRoleClaim<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    ///     Primary key
    /// </summary>
    public virtual int Id { get; set; }

    /// <summary>
    ///     User Id for the role this claim belongs to
    /// </summary>
    public virtual TKey RoleId { get; set; }

    /// <summary>
    ///     Claim type
    /// </summary>
    public virtual string ClaimType { get; set; }

    /// <summary>
    ///     Claim value
    /// </summary>
    public virtual string ClaimValue { get; set; }
}
