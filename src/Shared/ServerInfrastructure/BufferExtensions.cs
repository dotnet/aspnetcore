// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace System.Buffers;

internal static class BufferExtensions
{
    private const int _maxULongByteLength = 20;

    [ThreadStatic]
    private static byte[]? _numericBytesScratch;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> ToSpan(in this ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            return buffer.FirstSpan;
        }
        return buffer.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(in this ReadOnlySequence<byte> buffer, PipeWriter pipeWriter)
    {
        if (buffer.IsSingleSegment)
        {
            pipeWriter.Write(buffer.FirstSpan);
        }
        else
        {
            CopyToMultiSegment(buffer, pipeWriter);
        }
    }

    private static void CopyToMultiSegment(in ReadOnlySequence<byte> buffer, PipeWriter pipeWriter)
    {
        foreach (var item in buffer)
        {
            pipeWriter.Write(item.Span);
        }
    }

    public static ArraySegment<byte> GetArray(this Memory<byte> buffer)
    {
        return ((ReadOnlyMemory<byte>)buffer).GetArray();
    }

    public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory)
    {
        if (!MemoryMarshal.TryGetArray(memory, out var result))
        {
            throw new InvalidOperationException("Buffer backed by array was expected");
        }
        return result;
    }

    /// <summary>
    /// Returns position of first occurrence of item in the <see cref="ReadOnlySequence{T}"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SequencePosition? PositionOfAny<T>(in this ReadOnlySequence<T> source, T value0, T value1) where T : IEquatable<T>
    {
        if (source.IsSingleSegment)
        {
            int index = source.First.Span.IndexOfAny(value0, value1);
            if (index != -1)
            {
                return source.GetPosition(index);
            }

            return null;
        }
        else
        {
            return PositionOfAnyMultiSegment(source, value0, value1);
        }
    }

    private static SequencePosition? PositionOfAnyMultiSegment<T>(in ReadOnlySequence<T> source, T value0, T value1) where T : IEquatable<T>
    {
        SequencePosition position = source.Start;
        SequencePosition result = position;
        while (source.TryGet(ref position, out ReadOnlyMemory<T> memory))
        {
            int index = memory.Span.IndexOfAny(value0, value1);
            if (index != -1)
            {
                return source.GetPosition(index, result);
            }
            else if (position.GetObject() == null)
            {
                break;
            }

            result = position;
        }

        return null;
    }

    internal static void WriteAscii(ref this BufferWriter<PipeWriter> buffer, string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var dest = buffer.Span;
        var sourceLength = data.Length;
        // Fast path, try encoding to the available memory directly
        if (sourceLength <= dest.Length)
        {
            Encoding.ASCII.GetBytes(data, dest);
            buffer.Advance(sourceLength);
        }
        else
        {
            WriteEncodedMultiWrite(ref buffer, data, sourceLength, Encoding.ASCII);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void WriteNumeric(ref this BufferWriter<PipeWriter> buffer, ulong number)
    {
        const byte AsciiDigitStart = (byte)'0';

        var span = buffer.Span;
        var bytesLeftInBlock = span.Length;

        // Fast path, try copying to the available memory directly
        var simpleWrite = true;
        fixed (byte* output = span)
        {
            var start = output;
            if (number < 10 && bytesLeftInBlock >= 1)
            {
                *(start) = (byte)(((uint)number) + AsciiDigitStart);
                buffer.Advance(1);
            }
            else if (number < 100 && bytesLeftInBlock >= 2)
            {
                var val = (uint)number;
                var tens = (byte)((val * 205u) >> 11); // div10, valid to 1028

                *(start) = (byte)(tens + AsciiDigitStart);
                *(start + 1) = (byte)(val - (tens * 10) + AsciiDigitStart);
                buffer.Advance(2);
            }
            else if (number < 1000 && bytesLeftInBlock >= 3)
            {
                var val = (uint)number;
                var digit0 = (byte)((val * 41u) >> 12); // div100, valid to 1098
                var digits01 = (byte)((val * 205u) >> 11); // div10, valid to 1028

                *(start) = (byte)(digit0 + AsciiDigitStart);
                *(start + 1) = (byte)(digits01 - (digit0 * 10) + AsciiDigitStart);
                *(start + 2) = (byte)(val - (digits01 * 10) + AsciiDigitStart);
                buffer.Advance(3);
            }
            else
            {
                simpleWrite = false;
            }
        }

        if (!simpleWrite)
        {
            WriteNumericMultiWrite(ref buffer, number);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteNumericMultiWrite(ref this BufferWriter<PipeWriter> buffer, ulong number)
    {
        const byte AsciiDigitStart = (byte)'0';

        var value = number;
        var position = _maxULongByteLength;
        var byteBuffer = NumericBytesScratch;
        do
        {
            // Consider using Math.DivRem() if available
            var quotient = value / 10;
            byteBuffer[--position] = (byte)(AsciiDigitStart + (value - quotient * 10)); // 0x30 = '0'
            value = quotient;
        }
        while (value != 0);

        var length = _maxULongByteLength - position;
        buffer.Write(new ReadOnlySpan<byte>(byteBuffer, position, length));
    }

    internal static void WriteEncoded(ref this BufferWriter<PipeWriter> buffer, string data, Encoding encoding)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        var dest = buffer.Span;
        var sourceLength = encoding.GetByteCount(data);
        // Fast path, try encoding to the available memory directly
        if (sourceLength <= dest.Length)
        {
            encoding.GetBytes(data, dest);
            buffer.Advance(sourceLength);
        }
        else
        {
            WriteEncodedMultiWrite(ref buffer, data, sourceLength, encoding);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteEncodedMultiWrite(ref this BufferWriter<PipeWriter> buffer, string data, int encodedLength, Encoding encoding)
    {
        var source = data.AsSpan();
        var totalBytesUsed = 0;
        var encoder = encoding.GetEncoder();
        var minBufferSize = encoding.GetMaxByteCount(1);
        buffer.Ensure(minBufferSize);
        var bytes = buffer.Span;
        var completed = false;

        // This may be a bug, but encoder.Convert returns completed = true for UTF7 too early.
        // Therefore, we check encodedLength - totalBytesUsed too.
        while (!completed || encodedLength - totalBytesUsed != 0)
        {
            // Zero length spans are possible, though unlikely.
            // encoding.Convert and .Advance will both handle them so we won't special case for them.
            encoder.Convert(source, bytes, flush: true, out var charsUsed, out var bytesUsed, out completed);
            buffer.Advance(bytesUsed);

            totalBytesUsed += bytesUsed;
            if (totalBytesUsed >= encodedLength)
            {
                Debug.Assert(totalBytesUsed == encodedLength);
                // Encoded everything
                break;
            }

            source = source.Slice(charsUsed);

            // Get new span, more to encode.
            buffer.Ensure(minBufferSize);
            bytes = buffer.Span;
        }
    }

    private static byte[] NumericBytesScratch => _numericBytesScratch ?? CreateNumericBytesScratch();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] CreateNumericBytesScratch()
    {
        var bytes = new byte[_maxULongByteLength];
        _numericBytesScratch = bytes;
        return bytes;
    }
}
