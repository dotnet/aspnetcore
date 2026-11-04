// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

// This behaves like a List<TextChunk>, but is more optimized for growing to larger sizes
// since its underlying storage is pages rather than a single contiguous array. That means
// when expanding it doesn't have to copy the old data to a new location.
internal class TextChunkListBuilder(int pageLength)
{
    private TextChunkPage? _currentPage;
    private List<TextChunkPage>? _priorPages;

    public void Add(TextChunk value)
    {
        if (_currentPage is null)
        {
            _currentPage = new TextChunkPage(pageLength);
        }

        if (!_currentPage.TryAdd(value))
        {
            _priorPages ??= new();
            _priorPages.Add(_currentPage);
            _currentPage = new TextChunkPage(pageLength);
            if (!_currentPage.TryAdd(value))
            {
                throw new InvalidOperationException("New page didn't accept write");
            }
        }
    }

    public async Task WriteToAsync(TextWriter writer, string charArraySegments)
    {
        StringBuilder? tempBuffer = null;

        if (_priorPages is not null)
        {
            foreach (var page in _priorPages)
            {
                var (count, buffer) = (page.Count, page.Buffer);
                for (var i = 0; i < count; i++)
                {
                    await buffer[i].WriteToAsync(writer, charArraySegments, ref tempBuffer);
                }
            }
        }

        if (_currentPage is not null)
        {
            var (count, buffer) = (_currentPage.Count, _currentPage.Buffer);
            for (var i = 0; i < count; i++)
            {
                await buffer[i].WriteToAsync(writer, charArraySegments, ref tempBuffer);
            }
        }
    }

    public void Clear()
    {
        if (_currentPage is not null)
        {
            _currentPage = null;
        }

        _priorPages?.Clear();
    }
}
