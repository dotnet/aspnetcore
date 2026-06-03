// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Represents the JSON session configuration returned by the DBSC registration and refresh endpoints.
/// This instructs the browser on how to manage the device-bound session.
/// </summary>
public sealed class DeviceBoundSessionConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for the session.
    /// </summary>
    [JsonPropertyName("session_identifier")]
    public required string SessionIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the URL for future refresh requests.
    /// Can be relative to the registration/refresh URL.
    /// </summary>
    [JsonPropertyName("refresh_url")]
    public string? RefreshUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the session should continue.
    /// Set to <c>false</c> to terminate the session.
    /// </summary>
    [JsonPropertyName("continue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Continue { get; set; }

    /// <summary>
    /// Gets or sets the session scope configuration.
    /// </summary>
    [JsonPropertyName("scope")]
    public required DeviceBoundSessionScopeConfiguration Scope { get; set; }

    /// <summary>
    /// Gets or sets the list of credentials (cookies) protected by this session.
    /// </summary>
    [JsonPropertyName("credentials")]
    public required IList<DeviceBoundSessionCredentialConfiguration> Credentials { get; set; }

    /// <summary>
    /// Gets or sets the hosts allowed to initiate DBSC refreshes.
    /// </summary>
    [JsonPropertyName("allowed_refresh_initiators")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? AllowedRefreshInitiators { get; set; }
}

/// <summary>
/// Represents the scope configuration for a device-bound session.
/// </summary>
public sealed class DeviceBoundSessionScopeConfiguration
{
    /// <summary>
    /// Gets or sets the origin the session applies to.
    /// If not set, the origin of the registration/refresh URL is used.
    /// </summary>
    [JsonPropertyName("origin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Origin { get; set; }

    /// <summary>
    /// Gets or sets whether the session applies to the entire site (all subdomains)
    /// or just the origin.
    /// </summary>
    [JsonPropertyName("include_site")]
    public bool IncludeSite { get; set; }

    /// <summary>
    /// Gets or sets the scope specification rules.
    /// </summary>
    [JsonPropertyName("scope_specification")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<DeviceBoundSessionScopeRuleConfiguration>? ScopeSpecification { get; set; }
}

/// <summary>
/// Represents a scope rule in the session configuration JSON.
/// </summary>
public sealed class DeviceBoundSessionScopeRuleConfiguration
{
    /// <summary>
    /// Gets or sets the type: "include" or "exclude".
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the domain pattern.
    /// </summary>
    [JsonPropertyName("domain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the path prefix.
    /// </summary>
    [JsonPropertyName("path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }
}

/// <summary>
/// Represents a credential (cookie) protected by the device-bound session.
/// </summary>
public sealed class DeviceBoundSessionCredentialConfiguration
{
    /// <summary>
    /// Gets or sets the credential type. Must be "cookie".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "cookie";

    /// <summary>
    /// Gets or sets the name of the bound cookie.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the expected attributes of the cookie (e.g., "Domain=example.com; Secure; SameSite=Lax").
    /// </summary>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Attributes { get; set; }
}
