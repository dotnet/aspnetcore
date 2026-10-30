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

    // Backing storage for the CharArraySegment chunks in this list. Each list has its own
    // so that it can be reset when the list is cleared, without affecting the other list
    // that BufferedTextWriter may be writing to while this one is being flushed.
    private readonly StringBuilder _charArraySegments = new();
    private bool _charArraySegmentsNeedClear;

    public void Add(ReadOnlySpan<char> value)
    {
        if (_charArraySegmentsNeedClear)
        {
            // Deferred until needed because clearing a multi-chunk StringBuilder allocates,
            // which would be wasted work after the final flush.
            _charArraySegments.Clear();
            _charArraySegmentsNeedClear = false;
        }

        Add(new TextChunk(value, _charArraySegments));
    }

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

    public async Task WriteToAsync(TextWriter writer)
    {
        StringBuilder? tempBuffer = null;
        // If the buffer is pending a deferred clear, its content was already flushed and
        // no chunk in this list refers to it, so there's no need to copy it to a string.
        var charArraySegments = _charArraySegmentsNeedClear ? string.Empty : _charArraySegments.ToString();

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
        _charArraySegmentsNeedClear = _charArraySegments.Length > 0;
    }
}
