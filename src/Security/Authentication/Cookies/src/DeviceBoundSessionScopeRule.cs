// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Represents a scope specification rule for Device Bound Session Credentials.
/// Scope rules define which URL patterns are included or excluded from the DBSC session.
/// </summary>
public class DeviceBoundSessionScopeRule
{
    /// <summary>
    /// Gets or sets the type of scope rule: "include" or "exclude".
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the domain pattern for the rule.
    /// Supports wildcards (e.g., "*.example.com"). Defaults to "*" (all domains).
    /// </summary>
    public string Domain { get; set; } = "*";

    /// <summary>
    /// Gets or sets the path prefix for the rule.
    /// Defaults to "/" (all paths).
    /// </summary>
    public string Path { get; set; } = "/";
}
