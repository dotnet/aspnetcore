// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
