// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

// This behaves like a List<TextChunk>, but is more optimized for growing to larger sizes
// since its underlying storage is pages rather than a single contiguous array. That means
// when expanding it doesn't have to copy the old data to a new location.
internal class TextChunkListBuilder(int pageLength)
{
    private const int CharPageLength = 1024;

    private TextChunkPage? _currentPage;
    private List<TextChunkPage>? _priorPages;

    // Backing storage for text that is copied into this list (char/span writes and short strings).
    // Consecutive copied writes are coalesced into a single chunk, so that writing many small values
    // (e.g., '<', element names, attribute names, quotes) neither creates a chunk per value nor
    // requires a separate write to the underlying writer per value when flushing.
    // Each list has its own pages so that they can be reused when the list is cleared, without
    // affecting the other list that BufferedTextWriter may be writing to while this one is being flushed.
    private readonly List<char[]> _charPages = new();
    private int _charPageIndex;
    private int _charPagePosition;
    private int _openSegmentStart = -1;

    public void Add(ReadOnlySpan<char> value)
    {
        while (!value.IsEmpty)
        {
            if (_charPageIndex == _charPages.Count)
            {
                _charPages.Add(new char[CharPageLength]);
            }

            var page = _charPages[_charPageIndex];
            var copyLength = Math.Min(value.Length, CharPageLength - _charPagePosition);
            if (_openSegmentStart < 0)
            {
                _openSegmentStart = _charPagePosition;
            }

            value.Slice(0, copyLength).CopyTo(page.AsSpan(_charPagePosition));
            _charPagePosition += copyLength;
            value = value.Slice(copyLength);

            if (_charPagePosition == CharPageLength)
            {
                // Segments don't span pages, so close this one and continue on the next page
                CloseOpenSegment();
                _charPageIndex++;
                _charPagePosition = 0;
            }
        }
    }

    private void CloseOpenSegment()
    {
        if (_openSegmentStart >= 0)
        {
            AddCore(TextChunk.ForCharArraySegment(_charPageIndex, _openSegmentStart, _charPagePosition - _openSegmentStart));
            _openSegmentStart = -1;
        }
    }

    public void Add(TextChunk value)
    {
        CloseOpenSegment();
        AddCore(value);
    }

    private void AddCore(TextChunk value)
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
        CloseOpenSegment();

        StringBuilder? tempBuffer = null;

        if (_priorPages is not null)
        {
            foreach (var page in _priorPages)
            {
                var (count, buffer) = (page.Count, page.Buffer);
                for (var i = 0; i < count; i++)
                {
                    await buffer[i].WriteToAsync(writer, _charPages, ref tempBuffer);
                }
            }
        }

        if (_currentPage is not null)
        {
            var (count, buffer) = (_currentPage.Count, _currentPage.Buffer);
            for (var i = 0; i < count; i++)
            {
                await buffer[i].WriteToAsync(writer, _charPages, ref tempBuffer);
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
        _charPageIndex = 0;
        _charPagePosition = 0;
        _openSegmentStart = -1;
    }
}
