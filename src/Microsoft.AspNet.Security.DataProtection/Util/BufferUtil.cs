using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Security.DataProtection.Util
{
    internal static unsafe class BufferUtil
    {
        private static readonly byte[] _emptyArray = new byte[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlockCopy(IntPtr from, IntPtr to, int byteCount)
        {
            BlockCopy(from, to, checked((uint) byteCount)); // will be checked before invoking the delegate
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlockCopy(IntPtr from, IntPtr to, uint byteCount)
        {
            BlockCopySlow((byte*) from, (byte*) to, byteCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlockCopySlow(byte* from, byte* to, uint byteCount)
        {
            // slow, but works
            while (byteCount-- != 0)
            {
                *(to++) = *(from++);
            }
        }

        /// <summary>
        /// Creates a new managed byte[] from unmanaged memory.
        /// </summary>
        public static byte[] ToManagedByteArray(byte* ptr, int byteCount)
        {
            return ToManagedByteArray(ptr, checked((uint) byteCount));
        }

        /// <summary>
        /// Creates a new managed byte[] from unmanaged memory.
        /// </summary>
        public static byte[] ToManagedByteArray(byte* ptr, uint byteCount)
        {
            if (byteCount == 0)
            {
                return _emptyArray; // degenerate case
            }
            else
            {
                byte[] bytes = new byte[byteCount];
                fixed (byte* pBytes = bytes)
                {
                    BlockCopy(from: (IntPtr) ptr, to: (IntPtr) pBytes, byteCount: byteCount);
                }
                return bytes;
            }
        }

        /// <summary>
        /// Clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemory(byte* buffer, int byteCount)
        {
            ZeroMemory(buffer, checked((uint) byteCount));
        }

        /// <summary>
        /// Clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemory(byte* buffer, uint byteCount)
        {
            UnsafeNativeMethods.RtlZeroMemory((IntPtr) buffer, (UIntPtr) byteCount); // don't require 'checked': uint -> UIntPtr always guaranteed to succeed
        }
    }
}