// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

internal sealed class PagedCharBuffer : IDisposable
{
    public const int PageSize = 1024;
    private int _charIndex;

    public PagedCharBuffer(ICharBufferSource bufferSource)
    {
        BufferSource = bufferSource;
    }

    public ICharBufferSource BufferSource { get; }

    // Strongly typed rather than IList for performance
    public List<char[]> Pages { get; } = new List<char[]>();

    public int Length
    {
        get
        {
            var length = _charIndex;
            var pages = Pages;
            var fullPages = pages.Count - 1;
            for (var i = 0; i < fullPages; i++)
            {
                length += pages[i].Length;
            }

            return length;
        }
    }

    private char[] CurrentPage { get; set; }

    public void Append(char value)
    {
        var page = GetCurrentPage();
        page[_charIndex++] = value;
    }

    public void Append(string value)
    {
        if (value == null)
        {
            return;
        }

        var index = 0;
        var count = value.Length;

        while (count > 0)
        {
            var page = GetCurrentPage();
            var copyLength = Math.Min(count, page.Length - _charIndex);
            Debug.Assert(copyLength > 0);

            value.CopyTo(
                index,
                page,
                _charIndex,
                copyLength);

            _charIndex += copyLength;
            index += copyLength;

            count -= copyLength;
        }
    }

    public void Append(char[] buffer, int index, int count)
    {
        while (count > 0)
        {
            var page = GetCurrentPage();
            var copyLength = Math.Min(count, page.Length - _charIndex);
            Debug.Assert(copyLength > 0);

            Array.Copy(
                buffer,
                index,
                page,
                _charIndex,
                copyLength);

            _charIndex += copyLength;
            index += copyLength;
            count -= copyLength;
        }
    }

    /// <summary>
    /// Return all but one of the pages to the <see cref="ICharBufferSource"/>.
    /// This way if someone writes a large chunk of content, we can return those buffers and avoid holding them
    /// for extended durations.
    /// </summary>
    public void Clear()
    {
        var pages = Pages;
        for (var i = pages.Count - 1; i > 0; i--)
        {
            var page = pages[i];

            try
            {
                pages.RemoveAt(i);
            }
            finally
            {
                BufferSource.Return(page);
            }
        }

        _charIndex = 0;
        CurrentPage = pages.Count > 0 ? pages[0] : null;
    }

    private char[] GetCurrentPage()
    {
        if (CurrentPage == null || _charIndex == CurrentPage.Length)
        {
            CurrentPage = NewPage();
            _charIndex = 0;
        }

        return CurrentPage;
    }

    private char[] NewPage()
    {
        char[] page = null;
        try
        {
            page = BufferSource.Rent(PageSize);
            Pages.Add(page);
        }
        catch when (page != null)
        {
            BufferSource.Return(page);
            throw;
        }

        return page;
    }

    public void Dispose()
    {
        var pages = Pages;
        var count = pages.Count;
        for (var i = 0; i < count; i++)
        {
            BufferSource.Return(pages[i]);
        }

        pages.Clear();
    }
}
