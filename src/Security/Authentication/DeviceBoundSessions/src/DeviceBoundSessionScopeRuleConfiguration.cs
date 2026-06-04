// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Represents a scope specification rule in the DBSC session configuration.
/// </summary>
public sealed class DeviceBoundSessionScopeRuleConfiguration
{
    /// <summary>
    /// Gets or sets the type ("include" or "exclude").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    /// <summary>
    /// Gets or sets the domain pattern.
    /// </summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = default!;

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;
}
