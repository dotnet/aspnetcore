// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Represents a credential in the DBSC session instructions. Corresponds to the "JSON Session
/// Credential Format" defined in W3C Device Bound Session Credentials §9.9.
/// </summary>
public sealed class SessionCredential
{
    /// <summary>
    /// Gets or sets the credential type (always "cookie").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "cookie";

    /// <summary>
    /// Gets or sets the cookie name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the cookie attributes string.
    /// </summary>
    [JsonPropertyName("attributes")]
    public string Attributes { get; set; } = default!;
}
