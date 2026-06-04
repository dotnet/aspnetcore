// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Represents a scope rule for DBSC session configuration.
/// </summary>
public class DeviceBoundSessionScopeRule
{
    /// <summary>
    /// Gets or sets the type of scope rule ("include" or "exclude").
    /// </summary>
    public string Type { get; set; } = "include";

    /// <summary>
    /// Gets or sets the domain pattern for this scope rule.
    /// </summary>
    public string Domain { get; set; } = "*";

    /// <summary>
    /// Gets or sets the path for this scope rule.
    /// </summary>
    public string Path { get; set; } = "/";
}
