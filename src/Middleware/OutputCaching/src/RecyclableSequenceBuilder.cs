// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.OutputCaching;

// allows capture of written payloads into a ReadOnlySequence<byte> based on RecyclableReadOnlySequenceSegment
internal sealed class RecyclableSequenceBuilder : IDisposable
{
    private RecyclableReadOnlySequenceSegment? _firstSegment, _currentSegment;
    private int _currentSegmentIndex;
    private readonly int _segmentSize;
    private bool _closed;

    public long Length { get; private set; }

    internal RecyclableSequenceBuilder(int segmentSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentSize);

        _segmentSize = segmentSize;
    }

    // Extracting the buffered segments closes the stream for writing
    internal ReadOnlySequence<byte> DetachAndReset()
    {
        _closed = true;
        if (_firstSegment is null)
        {
            return default;
        }
        if (ReferenceEquals(_firstSegment, _currentSegment))
        {
            // single segment; use a simple sequence (no segments)
            ReadOnlyMemory<byte> memory = _firstSegment.Memory.Slice(0, _currentSegmentIndex);
            // we *can* recycle our single segment, just: keep the buffers
            RecyclableReadOnlySequenceSegment.RecycleChain(_firstSegment, recycleBuffers: false);
            // reset local state
            _firstSegment = _currentSegment = null;
            _currentSegmentIndex = 0;
            return new(memory);
        }

        // use a segmented sequence
        var payload = new ReadOnlySequence<byte>(_firstSegment, 0, _currentSegment!, _currentSegmentIndex);

        // reset our local state for an abundance of caution
        _firstSegment = _currentSegment = null;
        _currentSegmentIndex = 0;

        return payload;
    }

    public void Dispose() => RecyclableReadOnlySequenceSegment.RecycleChain(DetachAndReset(), recycleBuffers: true);

    private Span<byte> GetBuffer()
    {
        if (_closed)
        {
            Throw();
        }
        static void Throw() => throw new ObjectDisposedException(nameof(RecyclableSequenceBuilder), "The stream has been closed for writing.");

        if (_firstSegment is null)
        {
            _currentSegment = _firstSegment = RecyclableReadOnlySequenceSegment.Create(_segmentSize, null);
            _currentSegmentIndex = 0;
        }

        Debug.Assert(_currentSegment is not null);
        var current = _currentSegment.Memory;
        Debug.Assert(_currentSegmentIndex >= 0 && _currentSegmentIndex <= current.Length);

        if (_currentSegmentIndex == current.Length)
        {
            _currentSegment = RecyclableReadOnlySequenceSegment.Create(_segmentSize, _currentSegment);
            _currentSegmentIndex = 0;
            current = _currentSegment.Memory;
        }

        // have capacity in current chunk
        return MemoryMarshal.AsMemory(current).Span.Slice(_currentSegmentIndex);
    }
    public void Write(ReadOnlySpan<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            var available = GetBuffer();
            if (available.Length >= buffer.Length)
            {
                buffer.CopyTo(available);
                Advance(buffer.Length);
                return; // all done
            }
            else
            {
                var toWrite = Math.Min(buffer.Length, available.Length);
                if (toWrite <= 0)
                {
                    Throw();
                }
                buffer.Slice(0, toWrite).CopyTo(available);
                Advance(toWrite);
                buffer = buffer.Slice(toWrite);
            }
        }
        static void Throw() => throw new InvalidOperationException("Unable to acquire non-empty write buffer");
    }

    private void Advance(int count)
    {
        _currentSegmentIndex += count;
        Length += count;
    }

    public void WriteByte(byte value)
    {
        GetBuffer()[0] = value;
        Advance(1);
    }
}
