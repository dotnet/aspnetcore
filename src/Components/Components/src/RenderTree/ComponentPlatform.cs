// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides information about the platform that the component is running on.
/// </summary>
public sealed class ComponentPlatform
{
    /// <summary>
    /// Constructs a new instance of <see cref="ComponentPlatform"/>.
    /// </summary>
    /// <param name="platformName">The name of the platform.</param>
    /// <param name="isInteractive">A flag to indicate if the platform is interactive.</param>
    public ComponentPlatform(string platformName, bool isInteractive)
    {
        Name = platformName;
        IsInteractive = isInteractive;
    }

    /// <summary>
    /// Gets the name of the platform.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a flag to indicate if the platform is interactive.
    /// </summary>
    public bool IsInteractive { get; }
}
