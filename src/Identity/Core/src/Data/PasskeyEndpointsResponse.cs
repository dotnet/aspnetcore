// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The response body of the well-known passkey endpoints document.
/// </summary>
/// <remarks>
/// Members are omitted when unset. An empty document is valid, and signals support for passkeys
/// without advertising specific pages.
/// See <see href="https://w3c.github.io/webappsec-passkey-endpoints/"/>.
/// </remarks>
internal sealed class PasskeyEndpointsResponse
{
    [JsonPropertyName("enroll")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Enroll { get; init; }

    [JsonPropertyName("manage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Manage { get; init; }

    [JsonPropertyName("prfUsageDetails")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PrfUsageDetails { get; init; }
}
