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
