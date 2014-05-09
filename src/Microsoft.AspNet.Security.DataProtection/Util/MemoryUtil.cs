// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Security.DataProtection.Util
{
    internal unsafe static class MemoryUtil
    {
        /// <summary>
        /// Writes an Int32 to a potentially unaligned memory address, big-endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnalignedWriteBigEndian(byte* address, uint value)
        {
            *(address++) = (byte)(value >> 24);
            *(address++) = (byte)(value >> 16);
            *(address++) = (byte)(value >> 8);
            *(address) = (byte)value;
        }
    }
}
