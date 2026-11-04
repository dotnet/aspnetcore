// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a claim that is granted to all users within a role.
/// </summary>
/// <typeparam name="TKey">The type of the primary key of the role associated with this claim.</typeparam>
public class IdentityRoleClaim<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the identifier for this role claim.
    /// </summary>
    public virtual int Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the of the primary key of the role associated with this claim.
    /// </summary>
    public virtual TKey RoleId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the claim type for this claim.
    /// </summary>
    public virtual string? ClaimType { get; set; }

    /// <summary>
    /// Gets or sets the claim value for this claim.
    /// </summary>
    public virtual string? ClaimValue { get; set; }

    /// <summary>
    /// Constructs a new claim with the type and value.
    /// </summary>
    /// <returns>The <see cref="Claim"/> that was produced.</returns>
    public virtual Claim ToClaim()
    {
        return new Claim(ClaimType!, ClaimValue!);
    }

    /// <summary>
    /// Initializes by copying ClaimType and ClaimValue from the other claim.
    /// </summary>
    /// <param name="other">The claim to initialize from.</param>
    public virtual void InitializeFromClaim(Claim? other)
    {
        ClaimType = other?.Type;
        ClaimValue = other?.Value;
    }
}
