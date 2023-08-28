// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

// Acts as a supplier of TextChunkPage instances for TextChunkListBuilder
// Rather than directly use ArrayPool<T>.Shared, it has its own pool on top
// of that. This reduces cross-request contention.
internal readonly struct TextChunkPagePool(ArrayPool<TextChunk> underlyingPool, int pageLength) : IDisposable
{
    private readonly List<TextChunkPage> _allPages = new();
    private readonly Stack<TextChunkPage> _availablePages = new();

    public TextChunkPage Rent()
    {
        if (_availablePages.TryPop(out var existing))
        {
            return existing;
        }
        else
        {
            var newRawPage = underlyingPool.Rent(pageLength);
            var result = new TextChunkPage(newRawPage);
            _allPages.Add(result);
            return result;
        }
    }

    public void Return(TextChunkPage page)
    {
        page.Clear();
        _availablePages.Push(page);
    }

    public void Dispose()
    {
        foreach (var page in _allPages)
        {
            page.Clear();
            underlyingPool.Return(page.Buffer);
        }
    }
}
