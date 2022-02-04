// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.HttpLogging;

internal class BufferingWriter : IBufferWriter<byte>
{
    private const int MinimumBufferSize = 4096; // 4K
    private int _bytesBuffered;
    private BufferSegment? _head;
    private BufferSegment? _tail;
    private Memory<byte> _tailMemory; // remainder of tail memory
    private int _tailBytesBuffered;

    public int BytesBuffered
    {
        get { return _bytesBuffered; }
        set { _bytesBuffered = value; }
    }

    public int TailBytesBuffered
    {
        get { return _tailBytesBuffered; }
        set { _tailBytesBuffered = value; }
    }

    public Memory<byte> TailMemory
    {
        get { return _tailMemory; }
        set { _tailMemory = value; }
    }

    public BufferSegment? Head
    {
        get { return _head; }
        set { _head = value; }
    }

    public BufferSegment? Tail
    {
        get { return _tail; }
        set { _tail = value; }
    }

    public void Advance(int bytes)
    {
        if ((uint)bytes > (uint)_tailMemory.Length)
        {
            ThrowArgumentOutOfRangeException(nameof(bytes));
        }

        _tailBytesBuffered += bytes;
        _bytesBuffered += bytes;
        _tailMemory = _tailMemory.Slice(bytes);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        AllocateMemory(sizeHint);
        return _tailMemory;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        AllocateMemory(sizeHint);
        return _tailMemory.Span;
    }

    private void AllocateMemory(int sizeHint)
    {
        if (_head is null)
        {
            // We need to allocate memory to write since nobody has written before
            var newSegment = AllocateSegment(sizeHint);

            // Set all the pointers
            _head = _tail = newSegment;
            _tailBytesBuffered = 0;
        }
        else
        {
            var bytesLeftInBuffer = _tailMemory.Length;

            if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint)
            {
                Debug.Assert(_tail != null);

                if (_tailBytesBuffered > 0)
                {
                    // Flush buffered data to the segment
                    _tail.End += _tailBytesBuffered;
                    _tailBytesBuffered = 0;
                }

                var newSegment = AllocateSegment(sizeHint);

                _tail.SetNext(newSegment);
                _tail = newSegment;
            }
        }
    }

    private BufferSegment AllocateSegment(int sizeHint)
    {
        var newSegment = CreateSegment();

        // We can't use the recommended pool so use the ArrayPool
        newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(GetSegmentSize(sizeHint)));

        _tailMemory = newSegment.AvailableMemory;

        return newSegment;
    }

    private static BufferSegment CreateSegment()
    {
        return new BufferSegment();
    }

    private static int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
    {
        // First we need to handle case where hint is smaller than minimum segment size
        sizeHint = Math.Max(MinimumBufferSize, sizeHint);
        // After that adjust it to fit into pools max buffer size
        var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
        return adjustedToMaximumSize;
    }

    // Copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.Memory/src/System/ThrowHelper.cs
    private static void ThrowArgumentOutOfRangeException(string argumentName) { throw CreateArgumentOutOfRangeException(argumentName); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception CreateArgumentOutOfRangeException(string argumentName) { return new ArgumentOutOfRangeException(argumentName); }

}
