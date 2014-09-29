// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal unsafe static class BitHelpers
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
}
