// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

/// <summary>
/// A <see cref="IViewBufferScope"/> that uses pooled memory.
/// </summary>
internal sealed class MemoryPoolViewBufferScope : IViewBufferScope, IDisposable
{
    public const int MinimumSize = 16;
    private readonly ArrayPool<ViewBufferValue> _viewBufferPool;
    private readonly ArrayPool<char> _charPool;
    private List<ViewBufferValue[]> _available;
    private List<ViewBufferValue[]> _leased;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="MemoryPoolViewBufferScope"/>.
    /// </summary>
    /// <param name="viewBufferPool">
    /// The <see cref="ArrayPool{ViewBufferValue}"/> for creating <see cref="ViewBufferValue"/> instances.
    /// </param>
    /// <param name="charPool">
    /// The <see cref="ArrayPool{Char}"/> for creating <see cref="PagedBufferedTextWriter"/> instances.
    /// </param>
    public MemoryPoolViewBufferScope(ArrayPool<ViewBufferValue> viewBufferPool, ArrayPool<char> charPool)
    {
        _viewBufferPool = viewBufferPool;
        _charPool = charPool;
    }

    /// <inheritdoc />
    public ViewBufferValue[] GetPage(int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_leased == null)
        {
            _leased = new List<ViewBufferValue[]>(1);
        }

        ViewBufferValue[] segment = null;

        // Reuse pages that have been returned before going back to the memory pool.
        if (_available != null && _available.Count > 0)
        {
            segment = _available[_available.Count - 1];
            _available.RemoveAt(_available.Count - 1);
            return segment;
        }

        try
        {
            segment = _viewBufferPool.Rent(Math.Max(pageSize, MinimumSize));
            _leased.Add(segment);
        }
        catch when (segment != null)
        {
            _viewBufferPool.Return(segment);
            throw;
        }

        return segment;
    }

    /// <inheritdoc />
    public void ReturnSegment(ViewBufferValue[] segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        Array.Clear(segment, 0, segment.Length);

        if (_available == null)
        {
            _available = new List<ViewBufferValue[]>();
        }

        _available.Add(segment);
    }

    /// <inheritdoc />
    public TextWriter CreateWriter(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        return new PagedBufferedTextWriter(_charPool, writer);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            if (_leased == null)
            {
                return;
            }

            for (var i = 0; i < _leased.Count; i++)
            {
                _viewBufferPool.Return(_leased[i], clearArray: true);
            }

            _leased.Clear();
        }
    }
}
