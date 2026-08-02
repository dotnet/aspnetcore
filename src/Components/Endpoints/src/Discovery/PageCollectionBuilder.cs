// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// Represents the list of pages in a <see cref="ComponentApplicationBuilder"/>.
/// </summary>
internal class PageCollectionBuilder
{
    private readonly Dictionary<string, IReadOnlyList<PageComponentBuilder>> _pages = new();

    internal void Combine(PageCollectionBuilder pages)
    {
        foreach (var (assembly, pageCollection) in pages._pages)
        {
            if (!_pages.ContainsKey(assembly))
            {
                _pages.Add(assembly, pageCollection);
            }
        }
    }

    internal void Exclude(PageCollectionBuilder pages)
    {
        foreach (var (assembly, _) in pages._pages)
        {
            if (_pages.ContainsKey(assembly))
            {
                _pages.Remove(assembly);
            }
        }
    }

    internal void RemoveFromAssembly(string name)
    {
        _pages.Remove(name);
    }

    internal void AddFromLibraryInfo(string assemblyName, IReadOnlyList<PageComponentBuilder> pages)
    {
        _pages.Add(assemblyName, pages);
    }

    internal PageComponentInfo[] ToPageCollection()
    {
        var totalCount = 0;
        foreach (var value in _pages.Values)
        {
            totalCount += value.Count;
        }

        if (totalCount == 0)
        {
            return Array.Empty<PageComponentInfo>();
        }

        var list = new List<PageComponentInfo>(totalCount);
        // Reuse a buffer for computing the metadata
        var metadata = new List<object>();
        foreach (var (assembly, pages) in _pages)
        {
            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                ResolveMetadata(page.PageType!, metadata);
                var pageMetadata = metadata.ToArray();
                foreach (var route in page.RouteTemplates!)
                {
                    list.Add(page.Build(route, pageMetadata));
                }
            }
        }

        return list.ToArray();
    }

    private static void ResolveMetadata(Type componentType, List<object> result)
    {
        // We remove the route attribute since it is captured on the endpoint.
        // This is similar to how MVC behaves.
        // The RouteEndpoint already contains the information about the route
        // and since a page can get turned into multiple endpoints, its confusing
        // to have the route show up in the metadata.
        var attributes = componentType.GetCustomAttributes(inherit: true);
        result.Clear();
        foreach (var attribute in attributes)
        {
            if (attribute is RouteAttribute)
            {
                continue;
            }
            result.Add(attribute);
        }
    }
}
