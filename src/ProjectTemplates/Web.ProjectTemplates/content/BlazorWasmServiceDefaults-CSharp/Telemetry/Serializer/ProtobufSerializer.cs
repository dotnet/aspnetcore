// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace BlazorWasm.ServiceDefaults1.Telemetry.Serializer;

internal static class ProtobufSerializer
{
    private const uint UInt128 = 0x80;
    private const ulong ULong128 = 0x80;
    private const int MaskBitsLow = 0b_0111_1111;
    private const int MaskBitHigh = 0b_1000_0000;

    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetTagValue(int fieldNumber, ProtobufWireType wireType)
        => ((uint)(fieldNumber << 3)) | (uint)wireType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteTag(byte[] buffer, int writePosition, int fieldNumber, ProtobufWireType type)
        => WriteVarInt32(buffer, writePosition, GetTagValue(fieldNumber, type));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteLength(byte[] buffer, int writePosition, int length)
        => WriteVarInt32(buffer, writePosition, (uint)length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteTagAndLength(byte[] buffer, int writePosition, int contentLength, int fieldNumber, ProtobufWireType type)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, type);
        writePosition = WriteLength(buffer, writePosition, contentLength);
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteReservedLength(byte[] buffer, int writePosition, int length)
    {
        var slice = buffer.AsSpan(writePosition, 4);
        slice[0] = (byte)((length & MaskBitsLow) | MaskBitHigh);
        slice[1] = (byte)(((length >> 7) & MaskBitsLow) | MaskBitHigh);
        slice[2] = (byte)(((length >> 14) & MaskBitsLow) | MaskBitHigh);
        slice[3] = (byte)((length >> 21) & MaskBitsLow);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteBoolWithTag(byte[] buffer, int writePosition, int fieldNumber, bool value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.VARINT);
        buffer[writePosition++] = value ? (byte)1 : (byte)0;
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteEnumWithTag(byte[] buffer, int writePosition, int fieldNumber, int value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.VARINT);
        buffer[writePosition++] = (byte)value;
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed32LittleEndianFormat(byte[] buffer, int writePosition, uint value)
    {
        buffer[writePosition++] = (byte)value;
        buffer[writePosition++] = (byte)(value >> 8);
        buffer[writePosition++] = (byte)(value >> 16);
        buffer[writePosition++] = (byte)(value >> 24);
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed64LittleEndianFormat(byte[] buffer, int writePosition, ulong value)
    {
        buffer[writePosition++] = (byte)value;
        buffer[writePosition++] = (byte)(value >> 8);
        buffer[writePosition++] = (byte)(value >> 16);
        buffer[writePosition++] = (byte)(value >> 24);
        buffer[writePosition++] = (byte)(value >> 32);
        buffer[writePosition++] = (byte)(value >> 40);
        buffer[writePosition++] = (byte)(value >> 48);
        buffer[writePosition++] = (byte)(value >> 56);
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed32WithTag(byte[] buffer, int writePosition, int fieldNumber, uint value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.I32);
        writePosition = WriteFixed32LittleEndianFormat(buffer, writePosition, value);
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed64WithTag(byte[] buffer, int writePosition, int fieldNumber, ulong value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.I64);
        writePosition = WriteFixed64LittleEndianFormat(buffer, writePosition, value);
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarInt32(byte[] buffer, int writePosition, uint value)
    {
        while (value >= UInt128)
        {
            buffer[writePosition++] = (byte)(MaskBitHigh | (value & MaskBitsLow));
            value >>= 7;
        }

        buffer[writePosition++] = (byte)value;
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarInt64(byte[] buffer, int writePosition, ulong value)
    {
        while (value >= ULong128)
        {
            buffer[writePosition++] = (byte)(MaskBitHigh | (value & MaskBitsLow));
            value >>= 7;
        }

        buffer[writePosition++] = (byte)value;
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteInt64WithTag(byte[] buffer, int writePosition, int fieldNumber, ulong value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.VARINT);
        writePosition = WriteVarInt64(buffer, writePosition, value);
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteDoubleWithTag(byte[] buffer, int writePosition, int fieldNumber, double value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.I64);
        writePosition = WriteFixed64LittleEndianFormat(buffer, writePosition, (ulong)BitConverter.DoubleToInt64Bits(value));
        return writePosition;
    }

    internal static int ComputeVarInt64Size(ulong value)
    {
        return value switch
        {
            < 1UL << 7 => 1,
            < 1UL << 14 => 2,
            < 1UL << 21 => 3,
            < 1UL << 28 => 4,
            < 1UL << 35 => 5,
            < 1UL << 42 => 6,
            < 1UL << 49 => 7,
            < 1UL << 56 => 8,
            < 1UL << 63 => 9,
            _ => 10,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteByteArrayWithTag(byte[] buffer, int writePosition, int fieldNumber, ReadOnlySpan<byte> value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.LEN);
        writePosition = WriteLength(buffer, writePosition, value.Length);
        value.CopyTo(buffer.AsSpan(writePosition));
        writePosition += value.Length;
        return writePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetNumberOfUtf8CharsInString(ReadOnlySpan<char> value)
    {
        return s_utf8Encoding.GetByteCount(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteStringWithTag(byte[] buffer, int writePosition, int fieldNumber, string value)
    {
        return WriteStringWithTag(buffer, writePosition, fieldNumber, value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteStringWithTag(byte[] buffer, int writePosition, int fieldNumber, ReadOnlySpan<char> value)
    {
        var numberOfUtf8CharsInString = GetNumberOfUtf8CharsInString(value);
        return WriteStringWithTag(buffer, writePosition, fieldNumber, numberOfUtf8CharsInString, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteStringWithTag(byte[] buffer, int writePosition, int fieldNumber, int numberOfUtf8CharsInString, ReadOnlySpan<char> value)
    {
        writePosition = WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.LEN);
        writePosition = WriteLength(buffer, writePosition, numberOfUtf8CharsInString);

        _ = s_utf8Encoding.GetBytes(value, buffer.AsSpan(writePosition));
        writePosition += numberOfUtf8CharsInString;

        return writePosition;
    }
}
