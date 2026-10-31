// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.WebUtilities;

internal sealed class PagedByteBuffer : IDisposable
{
    internal const int PageSize = 1024;
    private readonly ArrayPool<byte> _arrayPool;
    private byte[]? _currentPage;
    private int _currentPageIndex;

    public PagedByteBuffer(ArrayPool<byte> arrayPool)
    {
        _arrayPool = arrayPool;
        Pages = new List<byte[]>();
    }

    public int Length { get; private set; }

    internal bool Disposed { get; private set; }

    internal List<byte[]> Pages { get; }

    private byte[] CurrentPage
    {
        get
        {
            if (_currentPage == null || _currentPageIndex == _currentPage.Length)
            {
                _currentPage = _arrayPool.Rent(PageSize);
                Pages.Add(_currentPage);
                _currentPageIndex = 0;
            }

            return _currentPage;
        }
    }

    public void Add(byte[] buffer, int offset, int count)
        => Add(buffer.AsMemory(offset, count));

    public void Add(ReadOnlyMemory<byte> memory)
    {
        ThrowIfDisposed();

        while (!memory.IsEmpty)
        {
            var currentPage = CurrentPage;
            var copyLength = Math.Min(memory.Length, currentPage.Length - _currentPageIndex);

            memory.Slice(0, copyLength).CopyTo(currentPage.AsMemory(_currentPageIndex, copyLength));

            Length += copyLength;
            _currentPageIndex += copyLength;

            memory = memory.Slice(copyLength);
        }
    }

    public void MoveTo(Stream stream)
    {
        ThrowIfDisposed();

        for (var i = 0; i < Pages.Count; i++)
        {
            var page = Pages[i];
            var length = (i == Pages.Count - 1) ?
                _currentPageIndex :
                page.Length;

            stream.Write(page, 0, length);
        }

        ClearBuffers();
    }

    public async Task MoveToAsync(PipeWriter writer, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        for (var i = 0; i < Pages.Count; i++)
        {
            var page = Pages[i];
            var length = (i == Pages.Count - 1) ?
                _currentPageIndex :
                page.Length;

            await writer.WriteAsync(page.AsMemory(0, length), cancellationToken);
        }

        ClearBuffers();
    }

    public async Task MoveToAsync(Stream stream, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        for (var i = 0; i < Pages.Count; i++)
        {
            var page = Pages[i];
            var length = (i == Pages.Count - 1) ?
                _currentPageIndex :
                page.Length;

            await stream.WriteAsync(page.AsMemory(0, length), cancellationToken);
        }

        ClearBuffers();
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            Disposed = true;
            ClearBuffers();
        }
    }

    private void ClearBuffers()
    {
        for (var i = 0; i < Pages.Count; i++)
        {
            _arrayPool.Return(Pages[i]);
        }

        Pages.Clear();
        _currentPage = null;
        Length = 0;
        _currentPageIndex = 0;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
    }
}
