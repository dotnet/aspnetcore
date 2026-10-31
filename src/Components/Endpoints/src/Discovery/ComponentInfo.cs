// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// Metadata captured for a component during discovery.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class ComponentInfo
{
    private IComponentRenderMode? _renderMode;

    /// <summary>
    /// Initializes a new instance of <see cref="ComponentInfo"/>.
    /// </summary>
    /// <param name="componentType">The component <see cref="Type"/>.</param>
    public ComponentInfo(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ComponentType = componentType;
    }

    /// <summary>
    /// Gets the component <see cref="Type"/>.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Gets the component <see cref="IComponentRenderMode"/>.
    /// </summary>
    public IComponentRenderMode? RenderMode
    {
        get => _renderMode;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            _renderMode = value;
        }
    }

    private string GetDebuggerDisplay()
    {
        var renderMode = GetRenderMode();

        return $"Type = {ComponentType.FullName}, {renderMode}";
    }

    private string GetRenderMode()
    {
        if (RenderMode is InteractiveServerRenderMode { Prerender: var server })
        {
            var size = (nameof(InteractiveServerRenderMode).Length - "RenderModeComparer".Length);
            return $"RenderModeComparer = {nameof(InteractiveServerRenderMode)[0..size]}, Prerendered = {server}";
        }
        if (RenderMode is InteractiveWebAssemblyRenderMode { Prerender: var wasm })
        {
            var size = (nameof(InteractiveWebAssemblyRenderMode).Length - "RenderModeComparer".Length);
            return $"RenderModeComparer = {nameof(InteractiveWebAssemblyRenderMode)[0..size]}, Prerendered = {wasm}";
        }
        if (RenderMode is InteractiveAutoRenderMode { Prerender: var auto })
        {
            var size = (nameof(InteractiveAutoRenderMode).Length - "RenderModeComparer".Length);
            return $"RenderModeComparer = {nameof(InteractiveAutoRenderMode)[0..size]}, Prerendered = {auto}";
        }

        return "RenderModeComparer = Unknown, Prerendered = Unknown";
    }
}
