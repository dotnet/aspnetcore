// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.OutputCaching;

internal ref struct FormatterBinaryReader
{
    // this is effectively a cut-down re-implementation of BinaryReader
    // from https://github.com/dotnet/runtime/blob/3689fbec921418e496962dc0ee252bdc9eafa3de/src/libraries/System.Private.CoreLib/src/System/IO/BinaryReader.cs
    // and is byte-compatible; however, instead of working against a Stream, we work against a ReadOnlyMemory<byte>
    //
    // additionally, we add support for reading a string with length specified by the caller (rather than handled automatically),
    // and in-place (zero-copy) BLOB reads

    private readonly ReadOnlyMemory<byte> _original; // used to allow us to zero-copy chunks out of the payload
    private readonly ref byte _root;
    private readonly int _length;
    private int _offset;

    public bool IsEOF => _offset >= _length;

    public FormatterBinaryReader(ReadOnlyMemory<byte> content)
    {
        _original = content;
        _length = content.Length;
        _root = ref MemoryMarshal.GetReference(content.Span);
    }

    public byte ReadByte()
    {
        if (IsEOF)
        {
            ThrowEndOfStream();
        }
        return Unsafe.Add(ref _root, _offset++);
    }

    public int Read7BitEncodedInt()
    {
        // Unlike writing, we can't delegate to the 64-bit read on
        // 64-bit platforms. The reason for this is that we want to
        // stop consuming bytes if we encounter an integer overflow.

        uint result = 0;
        byte byteReadJustNow;

        // Read the integer 7 bits at a time. The high bit
        // of the byte when on means to continue reading more bytes.
        //
        // There are two failure cases: we've read more than 5 bytes,
        // or the fifth byte is about to cause integer overflow.
        // This means that we can read the first 4 bytes without
        // worrying about integer overflow.

        const int MaxBytesWithoutOverflow = 4;
        for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            // ReadByte handles end of stream cases for us.
            byteReadJustNow = ReadByte();
            result |= (byteReadJustNow & 0x7Fu) << shift;

            if (byteReadJustNow <= 0x7Fu)
            {
                return (int)result; // early exit
            }
        }

        // Read the 5th byte. Since we already read 28 bits,
        // the value of this byte must fit within 4 bits (32 - 28),
        // and it must not have the high bit set.

        byteReadJustNow = ReadByte();
        if (byteReadJustNow > 0b_1111u)
        {
            ThrowOverflowException();
        }

        result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (int)result;
    }

    public long Read7BitEncodedInt64()
    {
        ulong result = 0;
        byte byteReadJustNow;

        // Read the integer 7 bits at a time. The high bit
        // of the byte when on means to continue reading more bytes.
        //
        // There are two failure cases: we've read more than 10 bytes,
        // or the tenth byte is about to cause integer overflow.
        // This means that we can read the first 9 bytes without
        // worrying about integer overflow.

        const int MaxBytesWithoutOverflow = 9;
        for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            // ReadByte handles end of stream cases for us.
            byteReadJustNow = ReadByte();
            result |= (byteReadJustNow & 0x7Ful) << shift;

            if (byteReadJustNow <= 0x7Fu)
            {
                return (long)result; // early exit
            }
        }

        // Read the 10th byte. Since we already read 63 bits,
        // the value of this byte must fit within 1 bit (64 - 63),
        // and it must not have the high bit set.

        byteReadJustNow = ReadByte();
        if (byteReadJustNow > 0b_1u)
        {
            ThrowOverflowException();
        }

        result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (long)result;
    }

    public string ReadString() => ReadString(Read7BitEncodedInt());

    public void SkipString() => Skip(Read7BitEncodedInt());

    public string ReadString(int bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);

        if (_offset > _length - bytes)
        {
            ThrowEndOfStream();
        }
        if (bytes == 0)
        {
            return "";
        }
        var s = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref _root, _offset), bytes));
        _offset += bytes;
        return s;
    }

    public void Skip(int bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        if (_offset > _length - bytes)
        {
            ThrowEndOfStream();
        }
        _offset += bytes;
    }

    public ReadOnlySpan<byte> ReadBytesSpan(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (_offset > _length - count)
        {
            ThrowEndOfStream();
        }
        if (count == 0)
        {
            return default;
        }
        var result = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref _root, _offset), count);
        _offset += count;
        return result;
    }

    public ReadOnlyMemory<byte> ReadBytesMemory(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (_offset > _length - count)
        {
            ThrowEndOfStream();
        }
        if (count == 0)
        {
            return default;
        }
        var result = _original.Slice(_offset, count);
        _offset += count;
        return result;
    }

    [DoesNotReturn]
    private static void ThrowEndOfStream() => throw new EndOfStreamException();

    [DoesNotReturn]
    private static void ThrowOverflowException() => throw new OverflowException();
}
