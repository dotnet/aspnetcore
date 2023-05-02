// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Determines how to handle render modes. This is intended for framework use only.
/// </summary>
public class RenderModeResolver
{
    /// <summary>
    /// Called by the framework to obtain a component instance. This is only called when a render mode is specified
    /// either at the call site or on the component type. Implementations for a particular hosting model may choose
    /// to return a component of a different type, or throw, depending on whether the hosting model supports the
    /// render mode and how it implements that support.
    /// </summary>
    /// <param name="componentType">The type of component that was requested.</param>
    /// <param name="componentActivator">An <see cref="IComponentActivator"/> that should be used when instantiating component objects.</param>
    /// <param name="componentTypeRenderMode">The <see cref="IComponentRenderMode"/> declared on <paramref name="componentType"/>, if any.</param>
    /// <param name="callSiteRenderMode">The <see cref="IComponentRenderMode"/> specified at the call site (for example, by the parent component), if any.</param>
    /// <returns></returns>
    public virtual IComponent ResolveComponent(
        Type componentType,
        IComponentActivator componentActivator,
        IComponentRenderMode? componentTypeRenderMode,
        IComponentRenderMode? callSiteRenderMode)
    {
        // Nothing is supported by default. Subclasses must override this to opt into supporting specific render modes.
        throw new NotSupportedException($"Cannot supply a component of type '{componentType}' because the current platform does not support the render mode {callSiteRenderMode ?? componentTypeRenderMode}.");
    }
}
