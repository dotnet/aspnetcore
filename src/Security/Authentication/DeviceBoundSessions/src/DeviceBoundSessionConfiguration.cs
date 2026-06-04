// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Represents the DBSC session configuration returned to the browser.
/// </summary>
public sealed class DeviceBoundSessionConfiguration
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    [JsonPropertyName("session_identifier")]
    public string SessionIdentifier { get; set; } = default!;

    /// <summary>
    /// Gets or sets the refresh URL.
    /// </summary>
    [JsonPropertyName("refresh_url")]
    public string? RefreshUrl { get; set; }

    /// <summary>
    /// Gets or sets the session scope.
    /// </summary>
    [JsonPropertyName("scope")]
    public DeviceBoundSessionScopeConfiguration? Scope { get; set; }

    /// <summary>
    /// Gets or sets the session credentials.
    /// </summary>
    [JsonPropertyName("credentials")]
    public List<DeviceBoundSessionCredentialConfiguration>? Credentials { get; set; }
}
