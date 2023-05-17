// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents the list of pages in a <see cref="ComponentApplicationBuilder"/>.
/// </summary>
internal class PageCollectionBuilder
{
    private readonly List<PageComponentBuilder> _pages = new();

    internal void Combine(PageCollectionBuilder pages)
    {
        for (var i = 0; i < pages._pages.Count; i++)
        {
            var pageToAdd = pages._pages[i];
            var found = false;
            for (var j = _pages.Count - 1; j > 0; j--)
            {
                var page = _pages[j];
                if (page.Equals(pageToAdd))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _pages.Add(pageToAdd);
            }
        }
    }

    internal void Exclude(PageCollectionBuilder pages)
    {
        for (var i = 0; i < pages._pages.Count; i++)
        {
            var pageToRemove = pages._pages[i];
            for (var j = _pages.Count - 1; j > 0; j--)
            {
                var page = _pages[j];
                if (page.Equals(pageToRemove))
                {
                    _pages.RemoveAt(j);
                    break;
                }
            }
        }
    }

    internal void RemoveFromAssembly(string name)
    {
        for (var i = _pages.Count - 1; i > 0; i--)
        {
            if (_pages[i].HasSource(name))
            {
                _pages.RemoveAt(i);
            }
        }
    }

    internal void AddFromLibraryInfo(IEnumerable<PageComponentBuilder> pages)
    {
        _pages.AddRange(pages);
    }

    internal PageComponentInfo[] ToPageCollection()
    {
        if (_pages.Count == 0)
        {
            return Array.Empty<PageComponentInfo>();
        }

        var list = new List<PageComponentInfo>();
        // Reuse a buffer for computing the metadata
        using var buffer = new MetadataBuffer(256);
        for (var i = 0; i < _pages.Count; i++)
        {
            var page = _pages[i];
            foreach (var route in page.RouteTemplates!)
            {
                list.Add(new PageComponentInfo(route, page.PageType!, route, ResolveMetadata(page.PageType!, buffer)));
            }
        }

        return list.ToArray();
    }

    private static IReadOnlyList<object> ResolveMetadata(Type componentType, MetadataBuffer buffer)
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

        public MetadataBuffer(int initialLength) : this()
        {
            Buffer = ArrayPool<object>.Shared.Rent(initialLength);
        }

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
            if (length > Buffer.Length)
            {
                ArrayPool<object>.Shared.Return(Buffer, clearArray: false);
            }
            else
            {
                Count = 0;
            }
        }

        internal object[] ToArray()
        {
            var result = new object[Count];
            Buffer.AsSpan(0, Count).CopyTo(result.AsSpan());
            return result;
        }
    }
}
