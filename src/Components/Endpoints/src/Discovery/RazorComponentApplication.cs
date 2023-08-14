// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// The definition of a component based application.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class RazorComponentApplication
{
    private readonly PageComponentInfo[] _pages;
    private readonly ComponentInfo[] _components;

    internal RazorComponentApplication(
        PageComponentInfo[] pageCollection,
        ComponentInfo[] componentCollection)
    {
        _pages = pageCollection;
        _components = componentCollection;
    }

    /// <summary>
    /// Gets the list of <see cref="PageComponentInfo"/> associated with the application.
    /// </summary>
    /// <returns>The list of pages.</returns>
    public IReadOnlyList<PageComponentInfo> Pages => _pages;

    /// <summary>
    /// Gets the list of <see cref="ComponentInfo"/> associated with the application.
    /// </summary>
    public IReadOnlyList<ComponentInfo> Components => _components;

    public ISet<IComponentRenderMode> GetDeclaredRenderModesByDiscoveredComponents()
    {
        var set = new HashSet<IComponentRenderMode>();
        for (var i = 0; i < Components.Count; i++)
        {
            var component = Components[i];
            switch (component.RenderMode)
            {
                case ServerRenderMode:
                    set.Add(RenderMode.Server);
                    break;
                case WebAssemblyRenderMode:
                    set.Add(RenderMode.WebAssembly);
                    break;
                case AutoRenderMode:
                    set.Add(RenderMode.Server);
                    set.Add(RenderMode.WebAssembly);
                    break;
                default:
                    break;
            }
        }

        return set;
    }

    private string GetDebuggerDisplay()
    {
        return $"Pages = {Pages.Count}, Components = {Components.Count}";
    }
}
