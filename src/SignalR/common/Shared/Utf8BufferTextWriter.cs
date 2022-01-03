// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.Internal;

internal sealed class Utf8BufferTextWriter : TextWriter
{
    private static readonly UTF8Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int MaximumBytesPerUtf8Char = 4;

    [ThreadStatic]
    private static Utf8BufferTextWriter? _cachedInstance;

    private readonly Encoder _encoder;
    private IBufferWriter<byte>? _bufferWriter;
    private Memory<byte> _memory;
    private int _memoryUsed;

#if DEBUG
    private bool _inUse;
#endif

    public override Encoding Encoding => _utf8NoBom;

    public Utf8BufferTextWriter()
    {
        _encoder = _utf8NoBom.GetEncoder();
    }

    public static Utf8BufferTextWriter Get(IBufferWriter<byte> bufferWriter)
    {
        var writer = _cachedInstance;
        if (writer == null)
        {
            writer = new Utf8BufferTextWriter();
        }

        // Taken off the thread static
        _cachedInstance = null;
#if DEBUG
        if (writer._inUse)
        {
            throw new InvalidOperationException("The writer wasn't returned!");
        }

        writer._inUse = true;
#endif
        writer.SetWriter(bufferWriter);
        return writer;
    }

    public static void Return(Utf8BufferTextWriter writer)
    {
        _cachedInstance = writer;

        writer._encoder.Reset();
        writer._memory = Memory<byte>.Empty;
        writer._memoryUsed = 0;
        writer._bufferWriter = null;

#if DEBUG
        writer._inUse = false;
#endif
    }

    public void SetWriter(IBufferWriter<byte> bufferWriter)
    {
        _bufferWriter = bufferWriter;
    }

    public override void Write(char[] buffer, int index, int count)
    {
        WriteInternal(buffer.AsSpan(index, count));
    }

    public override void Write(char[]? buffer)
    {
        if (buffer is not null)
        {
            WriteInternal(buffer);
        }
    }

    public override void Write(char value)
    {
        if (value <= 127)
        {
            EnsureBuffer();

            // Only need to set one byte
            // Avoid Memory<T>.Slice overhead for perf
            _memory.Span[_memoryUsed] = (byte)value;
            _memoryUsed++;
        }
        else
        {
            WriteMultiByteChar(value);
        }
    }

    private unsafe void WriteMultiByteChar(char value)
    {
        var destination = GetBuffer();

        // Json.NET only writes ASCII characters by themselves, e.g. {}[], etc
        // this should be an exceptional case
        var bytesUsed = 0;
        var charsUsed = 0;
#if NETCOREAPP
        _encoder.Convert(new Span<char>(&value, 1), destination, false, out charsUsed, out bytesUsed, out _);
#else
        fixed (byte* destinationBytes = &MemoryMarshal.GetReference(destination))
        {
            _encoder.Convert(&value, 1, destinationBytes, destination.Length, false, out charsUsed, out bytesUsed, out _);
        }
#endif

        Debug.Assert(charsUsed == 1);

        _memoryUsed += bytesUsed;
    }

    public override void Write(string? value)
    {
        if (value is not null)
        {
            WriteInternal(value.AsSpan());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> GetBuffer()
    {
        EnsureBuffer();

        return _memory.Span.Slice(_memoryUsed, _memory.Length - _memoryUsed);
    }

    private void EnsureBuffer()
    {
        // We need at least enough bytes to encode a single UTF-8 character, or Encoder.Convert will throw.
        // Normally, if there isn't enough space to write every character of a char buffer, Encoder.Convert just
        // writes what it can. However, if it can't even write a single character, it throws. So if the buffer has only
        // 2 bytes left and the next character to write is 3 bytes in UTF-8, an exception is thrown.
        var remaining = _memory.Length - _memoryUsed;
        if (remaining < MaximumBytesPerUtf8Char)
        {
            // Used up the memory from the buffer writer so advance and get more
            if (_memoryUsed > 0)
            {
                _bufferWriter!.Advance(_memoryUsed);
            }

            _memory = _bufferWriter!.GetMemory(MaximumBytesPerUtf8Char);
            _memoryUsed = 0;
        }
    }

    private void WriteInternal(ReadOnlySpan<char> buffer)
    {
        while (buffer.Length > 0)
        {
            // The destination byte array might not be large enough so multiple writes are sometimes required
            var destination = GetBuffer();

            var bytesUsed = 0;
            var charsUsed = 0;
#if NETCOREAPP
            _encoder.Convert(buffer, destination, false, out charsUsed, out bytesUsed, out _);
#else
            unsafe
            {
                fixed (char* sourceChars = &MemoryMarshal.GetReference(buffer))
                fixed (byte* destinationBytes = &MemoryMarshal.GetReference(destination))
                {
                    _encoder.Convert(sourceChars, buffer.Length, destinationBytes, destination.Length, false, out charsUsed, out bytesUsed, out _);
                }
            }
#endif

            buffer = buffer.Slice(charsUsed);
            _memoryUsed += bytesUsed;
        }
    }

    public override void Flush()
    {
        if (_memoryUsed > 0)
        {
            _bufferWriter!.Advance(_memoryUsed);
            _memory = _memory.Slice(_memoryUsed, _memory.Length - _memoryUsed);
            _memoryUsed = 0;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Flush();
        }
    }
}
