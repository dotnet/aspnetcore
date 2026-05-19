// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// Represents the list of components defined in a <see cref="ComponentApplicationBuilder"/>
/// before the configuration has finalized.
/// </summary>
internal class ComponentCollectionBuilder
{
    private readonly Dictionary<string, IReadOnlyList<ComponentBuilder>> _components = new();

    internal void Combine(ComponentCollectionBuilder components)
    {
        foreach (var (assembly, pageCollection) in components._components)
        {
            if (!_components.ContainsKey(assembly))
            {
                _components.Add(assembly, pageCollection);
            }
        }
    }

    internal void Exclude(ComponentCollectionBuilder components)
    {
        foreach (var (assembly, _) in components._components)
        {
            if (_components.ContainsKey(assembly))
            {
                _components.Remove(assembly);
            }
        }
    }

    internal void Remove(string name)
    {
        _components.Remove(name);
    }

    internal void AddFromLibraryInfo(string assemblyName, IReadOnlyList<ComponentBuilder> components)
    {
        _components.Add(assemblyName, components);
    }

    internal ComponentInfo[] ToComponentCollection()
    {
        var totalCount = 0;
        foreach (var value in _components.Values)
        {
            totalCount += value.Count;
        }

        if (totalCount == 0)
        {
            return Array.Empty<ComponentInfo>();
        }

        var current = 0;
        var result = new ComponentInfo[totalCount];
        foreach (var (_, components) in _components)
        {
            for (var i = 0; i < components.Count; i++, current++)
            {
                var componentType = components[i];
                result[current] = componentType.Build();
            }
        }

        return result;
    }
}
