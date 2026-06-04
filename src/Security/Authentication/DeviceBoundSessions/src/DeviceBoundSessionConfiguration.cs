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

/// <summary>
/// Represents the scope of a DBSC session.
/// </summary>
public sealed class DeviceBoundSessionScopeConfiguration
{
    /// <summary>
    /// Gets or sets the origin for the session scope.
    /// </summary>
    [JsonPropertyName("origin")]
    public string? Origin { get; set; }

    /// <summary>
    /// Gets or sets whether the session applies to the entire site.
    /// </summary>
    [JsonPropertyName("include_site")]
    public bool IncludeSite { get; set; }

    /// <summary>
    /// Gets or sets the scope specification rules.
    /// </summary>
    [JsonPropertyName("scope_specification")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DeviceBoundSessionScopeRuleConfiguration>? ScopeSpecification { get; set; }
}

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

/// <summary>
/// Represents a credential in the DBSC session configuration.
/// </summary>
public sealed class DeviceBoundSessionCredentialConfiguration
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

/// <summary>
/// Source-generated JSON serialization context for DBSC configuration types.
/// </summary>
[JsonSerializable(typeof(DeviceBoundSessionConfiguration))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class DeviceBoundSessionJsonContext : JsonSerializerContext
{
}
