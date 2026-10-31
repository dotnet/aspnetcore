// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.OutputCaching;

internal ref struct FormatterBinaryWriter
{
    // this is effectively a cut-down re-implementation of BinaryWriter
    // from https://github.com/dotnet/runtime/blob/3689fbec921418e496962dc0ee252bdc9eafa3de/src/libraries/System.Private.CoreLib/src/System/IO/BinaryWriter.cs
    // and is byte-compatible; however, instead of working against a Stream, we work against a IBufferWriter<byte>
    //
    // note it also has APIs for writing raw BLOBs

    private readonly IBufferWriter<byte> target;
    private int offset, length;
    private ref byte root;

    public FormatterBinaryWriter(IBufferWriter<byte> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        this.target = target;
        root = ref Unsafe.NullRef<byte>(); // no buffer initially
        offset = length = 0;
        DebugAssertValid();
    }

    private Span<byte> AvailableBuffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            DebugAssertValid();
            return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref root, offset), length - offset);
        }
    }

    [Conditional("DEBUG")]
    private void DebugAssertValid()
    {
        Debug.Assert(target is not null);
        if (Unsafe.IsNullRef(ref root))
        {
            // no buffer; expect all zeros
            Debug.Assert(length == 0 && offset == 0);
        }
        else
        {
            // have buffer; expect valid offset and positive length
            Debug.Assert(offset >= 0 && offset <= length);
            Debug.Assert(length > 0);
        }

    }

    // Writes a byte to this stream. The current position of the stream is
    // advanced by one.
    //
    public void Write(byte value)
    {
        if (offset < length)
        {
            Unsafe.Add(ref root, offset++) = value;
        }
        else
        {
            SlowWrite(value);
        }
        DebugAssertValid();
    }

    public void Write(string value) => Write(value, 0);

    internal void Write(string value, int lengthShift)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            Write(0); // length prefix
            return;
        }

        var bytes = Encoding.UTF8.GetByteCount(value);
        Write7BitEncodedInt(bytes << lengthShift); // length prefix
        if (bytes <= length - offset)
        {
            var actual = Encoding.UTF8.GetBytes(value, AvailableBuffer);
            Debug.Assert(actual == bytes);
            offset += bytes;
        }
        else
        {
            Flush();
            // get the encoding to do the heavy lifting directly
            var actual = Encoding.UTF8.GetBytes(value, target);
            Debug.Assert(actual == bytes);
        }
        DebugAssertValid();
    }

    private void RequestNewBuffer()
    {
        Flush();
        var span = target.GetSpan(1024); // fairly arbitrary non-trivial buffer; we can explore larger if useful
        if (span.IsEmpty)
        {
            Throw();
        }
        offset = 0;
        length = span.Length;
        root = ref MemoryMarshal.GetReference(span);

        DebugAssertValid();
        static void Throw() => throw new InvalidOperationException("Unable to acquire non-empty write buffer");
    }

    public void Flush() // commits the current buffer and leave in a buffer-free state
    {
        if (!Unsafe.IsNullRef(ref root))
        {
            target.Advance(offset);
            length = offset = 0;
            root = ref Unsafe.NullRef<byte>();
        }
        DebugAssertValid();
    }

    private void SlowWrite(byte value)
    {
        RequestNewBuffer();
        Unsafe.Add(ref root, offset++) = value;
    }

    public void Write7BitEncodedInt(int value)
    {
        uint uValue = (uint)value;

        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        //
        // Using the constants 0x7F and ~0x7F below offers smaller
        // codegen than using the constant 0x80.

        while (uValue > 0x7Fu)
        {
            Write((byte)(uValue | ~0x7Fu));
            uValue >>= 7;
        }

        Write((byte)uValue);
    }

    public void Write7BitEncodedInt64(long value)
    {
        ulong uValue = (ulong)value;

        // Write out an int 7 bits at a time. The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        //
        // Using the constants 0x7F and ~0x7F below offers smaller
        // codegen than using the constant 0x80.

        while (uValue > 0x7Fu)
        {
            Write((byte)((uint)uValue | ~0x7Fu));
            uValue >>= 7;
        }

        Write((byte)uValue);
    }

    public void WriteRaw(scoped ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        { } // nothing to do
        else if ((offset + value.Length) <= length)
        {
            value.CopyTo(AvailableBuffer);
            offset += value.Length;
        }
        else
        {
            SlowWriteRaw(value);
        }
        DebugAssertValid();
    }

    private void SlowWriteRaw(scoped ReadOnlySpan<byte> value)
    {
        do
        {
            RequestNewBuffer();
            var available = AvailableBuffer;
            var toWrite = Math.Min(value.Length, available.Length);
            value.Slice(start: 0, length: toWrite).CopyTo(available);
            offset += toWrite;
            value = value.Slice(start: toWrite);
        }
        while (!value.IsEmpty);
    }
}
