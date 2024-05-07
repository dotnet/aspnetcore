// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides information about the <see cref="IComponentRenderMode"/> a <see cref="IComponent"/> is currently running in.
/// </summary>
public class InteractiveHostRenderMode
{
    /// <summary>
    /// Initializes a new instance of <see cref="InteractiveHostRenderMode"/>.
    /// </summary>
    /// <param name="renderMode"></param>
    public InteractiveHostRenderMode(IComponentRenderMode renderMode)
    {
        ArgumentNullException.ThrowIfNull(renderMode, nameof(renderMode));

        RenderMode = renderMode;
    }

    /// <summary>
    /// Gets the <see cref="IComponentRenderMode"/> the component is currently running in.
    /// </summary>
    public IComponentRenderMode RenderMode { get; }
}
