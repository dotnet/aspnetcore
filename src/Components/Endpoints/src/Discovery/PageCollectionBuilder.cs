// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

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
        var buffer = new MetadataBuffer();
        try
        {
            foreach (var (assembly, pages) in _pages)
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    var page = pages[i];
                    var metadata = ResolveMetadata(page.PageType!, ref buffer);
                    foreach (var route in page.RouteTemplates!)
                    {
                        list.Add(new PageComponentInfo(route, page.PageType!, route, metadata));
                    }
                }
            }
        }
        finally
        {
            buffer.Dispose();
        }

        return list.ToArray();
    }

    private static IReadOnlyList<object> ResolveMetadata(Type componentType, ref MetadataBuffer buffer)
    {
        // We remove the route attribute since it is captured on the endpoint.
        // This is similar to how MVC behaves.
        var attributes = componentType.GetCustomAttributes(inherit: true);
        buffer.Reset(attributes.Length);
        foreach (var attribute in attributes)
        {
            if (attribute is RouteAttribute)
            {
                continue;
            }
            buffer.Add(attribute);
        }

        return buffer.ToArray();
    }

    private ref struct MetadataBuffer
    {
        public object[] Buffer;
        public int Count;

        public void Add(object element)
        {
            Buffer[Count++] = element;
        }

        public void Dispose()
        {
            if (Buffer != null)
            {
                ArrayPool<object>.Shared.Return(Buffer, clearArray: false);
            }
        }

        internal void Reset(int length)
        {
            if (Buffer == null)
            {
                Buffer = ArrayPool<object>.Shared.Rent(length);
            }
            else if (length > Buffer.Length)
            {
                ArrayPool<object>.Shared.Return(Buffer, clearArray: false);
                Buffer = ArrayPool<object>.Shared.Rent(length);
            }

            Count = 0;
        }

        internal readonly object[] ToArray()
        {
            var result = new object[Count];
            Buffer.AsSpan(0, Count).CopyTo(result.AsSpan());
            return result;
        }
    }
}
