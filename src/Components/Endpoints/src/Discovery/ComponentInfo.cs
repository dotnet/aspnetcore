// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents metadata about a component.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class ComponentInfo
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

        return $"{ComponentType.FullName}{renderMode}";
    }

    private string GetRenderMode()
    {
        if (RenderMode is ServerRenderMode { Prerender: var server })
        {
            return $" RenderMode: {RenderMode.GetType().Name[^"RenderMode".Length]}, Prerendered: {server}";
        }
        if (RenderMode is WebAssemblyRenderMode { Prerender: var wasm })
        {
            return $" RenderMode: {RenderMode.GetType().Name[^"RenderMode".Length]}, Prerendered: {wasm}";
        }
        if (RenderMode is AutoRenderMode { Prerender: var auto })
        {
            return $" RenderMode: {RenderMode.GetType().Name[^"RenderMode".Length]}, Prerendered: {auto}";
        }
        return "";
    }
}
