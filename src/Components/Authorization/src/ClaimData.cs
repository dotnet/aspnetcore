// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Authorization;

/// <summary>
/// This is a serializable representation of a <see cref="Claim"/> object that only consists of the type and value.
/// </summary>
public readonly struct ClaimData
{
    /// <summary>
    /// Constructs a new instance of <see cref="ClaimData"/> from a type and value.
    /// </summary>
    /// <param name="type">The claim type.</param>
    /// <param name="value">The claim value</param>
    [JsonConstructor]
    public ClaimData(string type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="ClaimData"/> from a <see cref="Claim"/> copying only the
    /// <see cref="Claim.Type"/> and <see cref="Claim.Value"/> into their corresponding properties.
    /// </summary>
    /// <param name="claim">The <see cref="Claim"/> to copy from.</param>
    public ClaimData(Claim claim)
        : this(claim.Type, claim.Value)
    {
    }

    /// <summary>
    /// Gets the claim type of the claim. <seealso cref="ClaimTypes"/>.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the value of the claim.
    /// </summary>
    public string Value { get; }
}
