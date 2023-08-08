// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * The implementation here matches the one present at https://github.com/dotnet/aspnetcore/blob/88180f6f487a1222b3af8c111aa6b5f8aa278633/src/Mvc/Mvc.ViewFeatures/src/Buffers/MemoryPoolViewBufferScope.cs
 * but avoids taking a depending on the IViewBufferScope interface to avoid circular dependencies between Mvc.Razor and Components.Endpoints.
 */

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// A <see cref="MemoryPoolViewBufferScope"/> that uses pooled memory.
/// </summary>
internal sealed class MemoryPoolViewBufferScope : IDisposable
{
    public const int MinimumSize = 16;
    private readonly ArrayPool<ViewBufferValue> _viewBufferPool;
    private List<ViewBufferValue[]>? _available;
    private List<ViewBufferValue[]>? _leased;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="MemoryPoolViewBufferScope"/>.
    /// </summary>
    /// <param name="viewBufferPool">
    /// The <see cref="ArrayPool{ViewBufferValue}"/> for creating <see cref="ViewBufferValue"/> instances.
    /// </param>
    public MemoryPoolViewBufferScope(ArrayPool<ViewBufferValue> viewBufferPool)
    {
        _viewBufferPool = viewBufferPool;
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

        ViewBufferValue[]? segment = null;

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
