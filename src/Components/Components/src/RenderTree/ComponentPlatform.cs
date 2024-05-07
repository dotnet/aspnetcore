// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

/// <summary>
/// Provide information about the platform that the component is running on.
/// </summary>
public class ComponentPlatform
{
    /// <summary>
    /// Initialize a new instance of <see cref="ComponentPlatform"/>.
    /// </summary>
    /// <param name="platformName">The name of the platform.</param>
    /// <param name="isInteractive">A flag to indicate if the platform is interactive.</param>
    /// <param name="renderMode">The <see cref="IComponentRenderMode"/> of the platform.</param>
    public ComponentPlatform(string platformName, bool isInteractive, IComponentRenderMode? renderMode)
    {
        PlatformName = platformName;
        IsInteractive = isInteractive;
        RenderMode = renderMode;
    }

    /// <summary>
    /// Gets the name of the platform.
    /// </summary>
    public string PlatformName { get; }

    /// <summary>
    /// Gets a flag to indicate if the platform is interactive.
    /// </summary>
    public bool IsInteractive { get; }

    /// <summary>
    /// Gets the <see cref="IComponentRenderMode"/> of the platform.
    /// </summary>
    public IComponentRenderMode? RenderMode { get; }
}
