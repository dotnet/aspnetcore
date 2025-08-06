// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection;

internal static class BitHelpers
{
    /// <summary>
    /// Reads an unsigned 64-bit integer from <paramref name="buffer"/>
    /// starting at offset <paramref name="offset"/>. Data is read big-endian.
    /// </summary>
    public static ulong ReadUInt64(byte[] buffer, int offset)
    {
        return (((ulong)buffer[offset + 0]) << 56)
               | (((ulong)buffer[offset + 1]) << 48)
               | (((ulong)buffer[offset + 2]) << 40)
               | (((ulong)buffer[offset + 3]) << 32)
               | (((ulong)buffer[offset + 4]) << 24)
               | (((ulong)buffer[offset + 5]) << 16)
               | (((ulong)buffer[offset + 6]) << 8)
               | (ulong)buffer[offset + 7];
    }

    /// <summary>
    /// Writes an unsigned 64-bit integer to <paramref name="buffer"/> starting at
    /// offset <paramref name="offset"/>. Data is written big-endian.
    /// </summary>
    public static void WriteUInt64(byte[] buffer, int offset, ulong value)
    {
        buffer[offset + 0] = (byte)(value >> 56);
        buffer[offset + 1] = (byte)(value >> 48);
        buffer[offset + 2] = (byte)(value >> 40);
        buffer[offset + 3] = (byte)(value >> 32);
        buffer[offset + 4] = (byte)(value >> 24);
        buffer[offset + 5] = (byte)(value >> 16);
        buffer[offset + 6] = (byte)(value >> 8);
        buffer[offset + 7] = (byte)(value);
    }
}
