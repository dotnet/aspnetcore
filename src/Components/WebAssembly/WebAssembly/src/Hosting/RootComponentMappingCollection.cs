// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// Defines a collection of <see cref="RootComponentMapping"/> items.
/// </summary>
public class RootComponentMappingCollection : Collection<RootComponentMapping>, IJSComponentConfiguration
{
    /// <inheritdoc />
    public JSComponentConfigurationStore JSComponents { get; } = new JSComponentConfigurationStore();

    /// <summary>
    /// Adds a component mapping to the collection.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="selector">The DOM element selector.</param>
    public void Add<[DynamicallyAccessedMembers(Component)] TComponent>(string selector) where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(selector);

        Add(new RootComponentMapping(typeof(TComponent), selector));
    }

    /// <summary>
    /// Adds a component mapping to the collection.
    /// </summary>
    /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
    /// <param name="selector">The DOM element selector.</param>
    public void Add([DynamicallyAccessedMembers(Component)] Type componentType, string selector)
    {
        Add(componentType, selector, ParameterView.Empty);
    }

    /// <summary>
    /// Adds a component mapping to the collection.
    /// </summary>
    /// <param name="componentType">The component type. Must implement <see cref="IComponent"/>.</param>
    /// <param name="selector">The DOM element selector.</param>
    /// <param name="parameters">The parameters to the root component.</param>
    public void Add([DynamicallyAccessedMembers(Component)] Type componentType, string selector, ParameterView parameters)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(selector);

        Add(new RootComponentMapping(componentType, selector, parameters));
    }

    /// <summary>
    /// Adds a collection of items to this collection.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<RootComponentMapping> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            Add(item);
        }
    }
}
