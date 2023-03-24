// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Web;

// This is for layering only. The main alternative would be making HtmlRendererCore public, but
// there's no use case for people using that directly, so we are instead adding an interface that
// represents just the parts of it we need in another layer.

/// <summary>
/// Represents the ability to supply prerendered content for a component. This is intended for
/// framework use and will not normally be required for use in applications.
/// </summary>
public interface IHtmlRendererContentProvider
{
    /// <summary>
    /// Gets the <see cref="Dispatcher"/> for the renderer.
    /// </summary>
    Dispatcher Dispatcher { get; }

    /// <summary>
    /// Gets the current render tree for a given component.
    /// </summary>
    /// <param name="componentId">The id for the component.</param>
    /// <returns>The frames representing the current render tree.</returns>
    ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId);
}
