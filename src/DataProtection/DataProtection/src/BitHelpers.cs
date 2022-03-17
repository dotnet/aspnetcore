// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.DataProtection;

internal static unsafe class BitHelpers
{
    /// <summary>
    /// Writes an unsigned 32-bit value to a memory address, big-endian.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTo(void* ptr, uint value)
    {
        byte* bytePtr = (byte*)ptr;
        bytePtr[0] = (byte)(value >> 24);
        bytePtr[1] = (byte)(value >> 16);
        bytePtr[2] = (byte)(value >> 8);
        bytePtr[3] = (byte)(value);
    }

    /// <summary>
    /// Writes an unsigned 32-bit value to a memory address, big-endian.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTo(ref byte* ptr, uint value)
    {
        byte* pTemp = ptr;
        pTemp[0] = (byte)(value >> 24);
        pTemp[1] = (byte)(value >> 16);
        pTemp[2] = (byte)(value >> 8);
        pTemp[3] = (byte)(value);
        ptr = &pTemp[4];
    }

    /// <summary>
    /// Writes a signed 32-bit value to a memory address, big-endian.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTo(byte[] buffer, ref int idx, int value)
    {
        WriteTo(buffer, ref idx, (uint)value);
    }

    /// <summary>
    /// Writes a signed 32-bit value to a memory address, big-endian.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTo(byte[] buffer, ref int idx, uint value)
    {
        buffer[idx++] = (byte)(value >> 24);
        buffer[idx++] = (byte)(value >> 16);
        buffer[idx++] = (byte)(value >> 8);
        buffer[idx++] = (byte)(value);
    }
}
