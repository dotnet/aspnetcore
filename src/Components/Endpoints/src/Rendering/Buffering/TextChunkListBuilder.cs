// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

// This behaves like a List<TextChunk>, but is more optimized for growing to larger sizes
// since its underlying storage is pages rather than a single contiguous array. That means
// when expanding it doesn't have to copy the old data to a new location.
internal class TextChunkListBuilder(ArrayPool<TextChunk> pool, int pageLength) : IDisposable
{
    private readonly TextChunkPagePool _pageSource = new TextChunkPagePool(pool, pageLength);
    private TextChunkPage? _currentPage;
    private List<TextChunkPage>? _priorPages;

    public void Add(TextChunk value)
    {
        if (_currentPage is null)
        {
            _currentPage = _pageSource.Rent();
        }

        if (!_currentPage.TryAdd(value))
        {
            _priorPages ??= new();
            _priorPages.Add(_currentPage);
            _currentPage = _pageSource.Rent();
            if (!_currentPage.TryAdd(value))
            {
                throw new InvalidOperationException("New page didn't accept write");
            }
        }
    }

    public async Task WriteToAsync(TextWriter writer)
    {
        StringBuilder? tempBuffer = null;

        if (_priorPages is not null)
        {
            foreach (var page in _priorPages)
            {
                var (count, buffer) = (page.Count, page.Buffer);
                for (var i = 0; i < count; i++)
                {
                    await buffer[i].WriteToAsync(writer, ref tempBuffer);
                }
            }
        }

        if (_currentPage is not null)
        {
            var (count, buffer) = (_currentPage.Count, _currentPage.Buffer);
            for (var i = 0; i < count; i++)
            {
                await buffer[i].WriteToAsync(writer, ref tempBuffer);
            }
        }
    }

    public void Clear()
    {
        if (_currentPage is not null)
        {
            _pageSource.Return(_currentPage);
            _currentPage = null;
        }

        if (_priorPages is not null)
        {
            foreach (var page in _priorPages)
            {
                _pageSource.Return(page);
            }

            _priorPages.Clear();
        }
    }

    public void Dispose()
    {
        Clear();
    }
}
