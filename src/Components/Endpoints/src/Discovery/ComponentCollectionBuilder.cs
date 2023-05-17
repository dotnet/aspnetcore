// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents the list of components defined in a <see cref="ComponentApplicationBuilder"/>
/// before the configuration has finalized.
/// </summary>
internal class ComponentCollectionBuilder
{
    private readonly List<ComponentBuilder> _components = new();

    internal void Combine(ComponentCollectionBuilder components)
    {
        for (var i = 0; i < components._components.Count; i++)
        {
            var pageToAdd = components._components[i];
            var found = false;
            for (var j = _components.Count - 1; j > 0; j--)
            {
                var page = _components[j];
                if (page.Equals(pageToAdd))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _components.Add(pageToAdd);
            }
        }
    }

    internal void Exclude(ComponentCollectionBuilder components)
    {
        for (var i = 0; i < components._components.Count; i++)
        {
            var pageToRemove = components._components[i];
            for (var j = _components.Count - 1; j > 0; j--)
            {
                var page = _components[j];
                if (page.Equals(pageToRemove))
                {
                    _components.RemoveAt(j);
                    break;
                }
            }
        }
    }

    internal void Remove(string name)
    {
        for (var i = _components.Count - 1; i > 0; i--)
        {
            if (_components[i].HasSource(name))
            {
                _components.RemoveAt(i);
            }
        }
    }

    internal void AddFromLibraryInfo(IEnumerable<ComponentBuilder> components)
    {
        _components.AddRange(components);
    }

    internal ComponentInfo[] ToComponentCollection()
    {
        if (_components.Count == 0)
        {
            return Array.Empty<ComponentInfo>();
        }

        var result = new ComponentInfo[_components.Count];
        for (var i = 0; i < _components.Count; i++)
        {
            var componentType = _components[i];
            if (componentType.RenderMode != null)
            {
                result[i] = new ComponentInfo(componentType.ComponentType)
                {
                    RenderMode = componentType.RenderMode.Mode,
                };
            }
            else
            {
                result[i] = new ComponentInfo(componentType.ComponentType);
            }
        }

        return result;
    }
}
