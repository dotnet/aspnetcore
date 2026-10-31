// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers;

internal sealed class BufferSegment : ReadOnlySequenceSegment<byte>
{
    public BufferSegment(Memory<byte> memory)
    {
        Memory = memory;
    }

    public BufferSegment Append(Memory<byte> memory)
    {
        var segment = new BufferSegment(memory)
        {
            RunningIndex = RunningIndex + Memory.Length
        };
        Next = segment;
        return segment;
    }
}
