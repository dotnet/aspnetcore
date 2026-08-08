// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// The definition of a component based application.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class RazorComponentApplication
{
    private readonly PageComponentDescriptor[] _pages;
    private readonly ComponentDescriptor[] _components;

    internal RazorComponentApplication(
        PageComponentDescriptor[] pageCollection,
        ComponentDescriptor[] componentCollection)
    {
        _pages = pageCollection;
        _components = componentCollection;
    }

    /// <summary>
    /// Gets the list of <see cref="PageComponentDescriptor"/> associated with the application.
    /// </summary>
    /// <returns>The list of pages.</returns>
    public IReadOnlyList<PageComponentDescriptor> Pages => _pages;

    /// <summary>
    /// Gets the list of <see cref="ComponentDescriptor"/> associated with the application.
    /// </summary>
    public IReadOnlyList<ComponentDescriptor> Components => _components;

    public ISet<IComponentRenderMode> GetDeclaredRenderModesByDiscoveredComponents()
    {
        var set = new HashSet<IComponentRenderMode>();
        for (var i = 0; i < Components.Count; i++)
        {
            switch (GetRenderMode(Components[i]))
            {
                case InteractiveServerRenderMode:
                    set.Add(RenderMode.InteractiveServer);
                    break;
                case InteractiveWebAssemblyRenderMode:
                    set.Add(RenderMode.InteractiveWebAssembly);
                    break;
                case InteractiveAutoRenderMode:
                    set.Add(RenderMode.InteractiveServer);
                    set.Add(RenderMode.InteractiveWebAssembly);
                    break;
                default:
                    break;
            }
        }

        return set;

        // The render mode is carried as a RenderModeAttribute in the descriptor's metadata rather than
        // as a named member, which is the same shape a reflective attribute lookup produces and keeps
        // the descriptor open to further routing and rendering attributes.
        static IComponentRenderMode? GetRenderMode(ComponentDescriptor descriptor)
        {
            for (var i = 0; i < descriptor.Metadata.Count; i++)
            {
                if (descriptor.Metadata[i] is RenderModeAttribute renderModeAttribute)
                {
                    return renderModeAttribute.Mode;
                }
            }

            return null;
        }
    }

    private string GetDebuggerDisplay()
    {
        return $"Pages = {Pages.Count}, Components = {Components.Count}";
    }
}
