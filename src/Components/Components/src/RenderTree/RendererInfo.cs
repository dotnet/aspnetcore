// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides information about the platform that the component is running on.
/// </summary>
/// <param name="rendererName">The name of the platform.</param>
/// <param name="isInteractive">A flag to indicate if the platform is interactive.</param>
public sealed class RendererInfo(string rendererName, bool isInteractive)
{
    /// <summary>
    /// Gets the name of the platform.
    /// </summary>
    public string Name { get; } = rendererName;

    /// <summary>
    /// Gets a flag to indicate if the platform is interactive.
    /// </summary>
    public bool IsInteractive { get; } = isInteractive;
}
