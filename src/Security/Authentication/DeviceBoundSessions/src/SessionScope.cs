// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Represents the scope of a DBSC session. Corresponds to the "JSON Session Scope Instruction
/// Format" defined in W3C Device Bound Session Credentials §9.7.
/// </summary>
public sealed class SessionScope
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
    public List<SessionScopeRule>? ScopeSpecification { get; set; }
}
