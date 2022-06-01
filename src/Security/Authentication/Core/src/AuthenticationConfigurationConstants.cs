// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Constants for managing generating authentication options from
/// configuration.
/// </summary>
public static class AuthenticationConfigurationConstants
{
    /// <summary>
    /// The top-level "Authentication" key.
    /// </summary>
    public const string Authentication = "Authentication";
    /// <summary>
    /// The "Schemes" key for registering schemes.
    /// </summary>
    public const string Schemes = "Schemes";
    /// <summary>
    /// The "DefaultScheme" key.
    /// </summary>
    public const string DefaultScheme = "DefaultScheme";
}
