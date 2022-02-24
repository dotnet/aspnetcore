// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Provides programmatic configuration for localization.
/// </summary>
public class LocalizationOptions
{
    /// <summary>
    /// Creates a new <see cref="LocalizationOptions" />.
    /// </summary>
    public LocalizationOptions()
    { }

    /// <summary>
    /// The relative path under application root where resource files are located.
    /// </summary>
    public string ResourcesPath { get; set; } = string.Empty;
}
