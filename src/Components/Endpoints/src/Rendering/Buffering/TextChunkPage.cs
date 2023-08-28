// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

// Used internally by TextChunkListBuilder
internal class TextChunkPage(TextChunk[] _buffer)
{
    private int _count;

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
        Array.Clear(_buffer, 0, _count);
        _count = 0;
    }
}
