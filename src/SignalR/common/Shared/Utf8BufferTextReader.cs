// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class Utf8BufferTextReader : TextReader
{
    private readonly Decoder _decoder;
    private ReadOnlySequence<byte> _utf8Buffer;

    [ThreadStatic]
    private static Utf8BufferTextReader? _cachedInstance;

#if DEBUG
    private bool _inUse;
#endif

    public Utf8BufferTextReader()
    {
        _decoder = Encoding.UTF8.GetDecoder();
    }

    public static Utf8BufferTextReader Get(in ReadOnlySequence<byte> utf8Buffer)
    {
        var reader = _cachedInstance;
        if (reader == null)
        {
            reader = new Utf8BufferTextReader();
        }

        // Taken off the thread static
        _cachedInstance = null;
#if DEBUG
        if (reader._inUse)
        {
            throw new InvalidOperationException("The reader wasn't returned!");
        }

        reader._inUse = true;
#endif
        reader.SetBuffer(utf8Buffer);
        return reader;
    }

    public static void Return(Utf8BufferTextReader reader)
    {
        _cachedInstance = reader;
#if DEBUG
        reader._inUse = false;
#endif
    }

    public void SetBuffer(in ReadOnlySequence<byte> utf8Buffer)
    {
        _utf8Buffer = utf8Buffer;
        _decoder.Reset();
    }

    public override int Read(char[] buffer, int index, int count)
    {
        if (_utf8Buffer.IsEmpty)
        {
            return 0;
        }

        var source = _utf8Buffer.First.Span;
        var bytesUsed = 0;
        var charsUsed = 0;
#if NETCOREAPP
        var destination = new Span<char>(buffer, index, count);
        _decoder.Convert(source, destination, false, out bytesUsed, out charsUsed, out var completed);
#else
        unsafe
        {
            fixed (char* destinationChars = &buffer[index])
            fixed (byte* sourceBytes = &MemoryMarshal.GetReference(source))
            {
                _decoder.Convert(sourceBytes, source.Length, destinationChars, count, false, out bytesUsed, out charsUsed, out var completed);
            }
        }
#endif
        _utf8Buffer = _utf8Buffer.Slice(bytesUsed);

        return charsUsed;
    }
}
