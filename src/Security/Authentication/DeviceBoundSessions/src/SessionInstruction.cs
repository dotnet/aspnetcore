// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Represents the DBSC JSON session instructions returned to the browser during registration
/// and (optionally) refresh. Corresponds to the "JSON Session Instruction Format" defined in
/// W3C Device Bound Session Credentials §9.6.
/// </summary>
public sealed class SessionInstruction
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
    public SessionScope? Scope { get; set; }

    /// <summary>
    /// Gets or sets the session credentials.
    /// </summary>
    [JsonPropertyName("credentials")]
    public List<SessionCredential>? Credentials { get; set; }
}
