// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// Represents the scope of a DBSC session. Corresponds to the "JSON Session Scope Instruction
/// Format" defined in W3C Device Bound Session Credentials §9.7.
/// </summary>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal sealed class SessionScope
{
    /// <summary>
    /// Gets or sets the origin for the session scope.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("origin")]
    internal string? Origin { get; set; }

    /// <summary>
    /// Gets or sets whether the session applies to the entire site.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("include_site")]
    internal bool IncludeSite { get; set; }

    /// <summary>
    /// Gets or sets the scope specification rules. See W3C Device Bound Session Credentials §9.7.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("scope_specification")]
    internal List<SessionScopeRule> ScopeSpecification { get; set; } = [];
}
