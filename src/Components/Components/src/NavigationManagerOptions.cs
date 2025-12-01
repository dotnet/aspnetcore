// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides configuration for <see cref="NavigationManager"/> behavior.
/// </summary>
public class NavigationManagerOptions
{
    /// <summary>
    /// Gets or sets the string comparison used when comparing URIs against the base URI.
    /// The default is <see cref="System.StringComparison.Ordinal"/> for backward compatibility.
    /// Set to <see cref="System.StringComparison.OrdinalIgnoreCase"/> to enable case-insensitive matching.
    /// </summary>
    public StringComparison PathBaseComparison { get; set; } = StringComparison.Ordinal;
}
