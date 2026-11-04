// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.OAuth.Claims;

/// <summary>
/// A ClaimAction that selects a second level value from the json user data with the given top level key
/// name and second level sub key name and add it as a Claim.
/// This no-ops if the keys are not found or the value is empty.
/// </summary>
public class JsonSubKeyClaimAction : JsonKeyClaimAction
{
    /// <summary>
    /// Creates a new JsonSubKeyClaimAction.
    /// </summary>
    /// <param name="claimType">The value to use for Claim.Type when creating a Claim.</param>
    /// <param name="valueType">The value to use for Claim.ValueType when creating a Claim.</param>
    /// <param name="jsonKey">The top level key to look for in the json user data.</param>
    /// <param name="subKey">The second level key to look for in the json user data.</param>
    public JsonSubKeyClaimAction(string claimType, string valueType, string jsonKey, string subKey)
        : base(claimType, valueType, jsonKey)
    {
        SubKey = subKey;
    }

    /// <summary>
    /// The second level key to look for in the json user data.
    /// </summary>
    public string SubKey { get; }

    /// <inheritdoc />
    public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
    {
        if (!TryGetSubProperty(userData, JsonKey, SubKey, out var value))
        {
            return;
        }
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in value.EnumerateArray())
            {
                AddClaim(v.ToString()!, identity, issuer);
            }
        }
        else
        {
            AddClaim(value.ToString()!, identity, issuer);
        }
    }

    // Get the given subProperty from a property.
    private static bool TryGetSubProperty(JsonElement userData, string propertyName, string subProperty, out JsonElement value)
    {
        return userData.TryGetProperty(propertyName, out value)
            && value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty(subProperty, out value);
    }
}
