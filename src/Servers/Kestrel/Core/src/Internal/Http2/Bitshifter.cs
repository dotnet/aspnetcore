// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    // Mimics BinaryPrimitives with oddly sized units
    internal static class Bitshifter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt24BigEndian(ReadOnlySpan<byte> source)
        {
            return (uint)((source[0] << 16) | (source[1] << 8) | source[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt24BigEndian(Span<byte> destination, uint value)
        {
            Debug.Assert(value <= 0xFF_FF_FF, value.ToString());
            destination[0] = (byte)((value & 0xFF_00_00) >> 16);
            destination[1] = (byte)((value & 0x00_FF_00) >> 8);
            destination[2] = (byte)(value & 0x00_00_FF);
        }

        // Drops the highest order bit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt31BigEndian(ReadOnlySpan<byte> source)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(source) & 0x7F_FF_FF_FF;
        }

        // Does not overwrite the highest order bit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt31BigEndian(Span<byte> destination, uint value)
            => WriteUInt31BigEndian(destination, value, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt31BigEndian(Span<byte> destination, uint value, bool preserveHighestBit)
        {
            Debug.Assert(value <= 0x7F_FF_FF_FF, value.ToString());

            if (preserveHighestBit)
            {
                // Keep the highest bit
                value |= (destination[0] & 0x80u) << 24;
            }

            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        }
    }
}
