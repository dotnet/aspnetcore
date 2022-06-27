// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a claim that a user possesses.
/// </summary>
/// <typeparam name="TKey">The type used for the primary key for this user that possesses this claim.</typeparam>
public class IdentityUserClaim<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the identifier for this user claim.
    /// </summary>
    public virtual int Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the primary key of the user associated with this claim.
    /// </summary>
    public virtual TKey UserId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the claim type for this claim.
    /// </summary>
    public virtual string? ClaimType { get; set; }

    /// <summary>
    /// Gets or sets the claim value for this claim.
    /// </summary>
    public virtual string? ClaimValue { get; set; }

    /// <summary>
    /// Converts the entity into a Claim instance.
    /// </summary>
    /// <returns></returns>
    public virtual Claim ToClaim()
    {
        return new Claim(ClaimType!, ClaimValue!);
    }

    /// <summary>
    /// Reads the type and value from the Claim.
    /// </summary>
    /// <param name="claim"></param>
    public virtual void InitializeFromClaim(Claim claim)
    {
        ClaimType = claim.Type;
        ClaimValue = claim.Value;
    }
}
