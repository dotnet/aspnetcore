// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

// Used internally by TextChunkListBuilder
internal class TextChunkPage
{
    private readonly TextChunk[] _buffer;
    private int _count;

    public TextChunkPage(int capacity)
    {
        // Note: we did try using array pooling here, both via ArrayPool<>.Shared and with a per-request
        // pool like MVC's ViewBufferTextWriter. None of them changed the overall throughput. It may be
        // that the cost of contention across the pool, and having to clear arrays before returning to
        // pools, balances out with the GC cost of simply instantiating new buffers.
        // Since there was no clear observable improvement from pooling, this code now uses the simpler
        // "just instantiate" strategy and avoids the need for being careful about disposal.
        _buffer = new TextChunk[capacity];
    }

    public TextChunk[] Buffer => _buffer;
    public int Count => _count;

    public bool TryAdd(TextChunk value)
    {
        if (_count < _buffer.Length)
        {
            _buffer[_count++] = value;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Clear()
    {
        _count = 0;
    }
}
