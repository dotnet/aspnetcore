// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims;

/// <summary>
/// Infrastructure for mapping user data from a json structure to claims on the ClaimsIdentity.
/// </summary>
public abstract class ClaimAction
{
    /// <summary>
    /// Create a new claim manipulation action.
    /// </summary>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
    public ClaimAction(string claimType, string valueType)
    {
        ClaimType = claimType;
        ValueType = valueType;
    }

    /// <summary>
    /// Gets the value to use for <see cref="Claim.Value"/>when creating a Claim.
    /// </summary>
    public string ClaimType { get; }

    /// <summary>
    /// Gets the value to use for <see cref="Claim.ValueType"/> when creating a Claim.
    /// </summary>
    public string ValueType { get; }

    /// <summary>
    /// Examine the given userData JSON, determine if the requisite data is present, and optionally add it
    /// as a new Claim on the ClaimsIdentity.
    /// </summary>
    /// <param name="userData">The source data to examine. This value may be null.</param>
    /// <param name="identity">The identity to add Claims to.</param>
    /// <param name="issuer">The value to use for Claim.Issuer when creating a Claim.</param>
    public abstract void Run(JsonElement userData, ClaimsIdentity identity, string issuer);
}
