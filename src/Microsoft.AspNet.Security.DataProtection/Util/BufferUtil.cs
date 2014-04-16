using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection.Util
{
    internal unsafe static class BufferUtil
    {
        private static readonly byte[] _emptyArray = new byte[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlockCopy(void* from, void* to, int byteCount)
        {
            BlockCopy(from, to, checked((uint)byteCount)); // will be checked before invoking the delegate
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlockCopy(void* from, void* to, uint byteCount)
        {
            if (byteCount != 0)
            {
#if NET45
                BlockCopySlow((byte*)from, (byte*)to, byteCount);
#else
                Buffer.MemoryCopy(source: from, destination: to, destinationSizeInBytes: byteCount, sourceBytesToCopy: byteCount);
#endif
            }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BlockCopySlow(byte* from, byte* to, uint byteCount)
        {
            while (byteCount-- != 0)
            {
                *(to++) = *(from++);
            }
        }
#endif

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SecureZeroMemory(byte* buffer, int byteCount)
        {
            SecureZeroMemory(buffer, checked((uint)byteCount));
        }

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SecureZeroMemory(byte* buffer, uint byteCount)
        {
            UnsafeNativeMethods.RtlZeroMemory((IntPtr)buffer, (UIntPtr)byteCount);
        }

        /// <summary>
        /// Creates a new managed byte[] from unmanaged memory.
        /// </summary>
        public static byte[] ToManagedByteArray(byte* ptr, int byteCount)
        {
            return ToManagedByteArray(ptr, checked((uint)byteCount));
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
                    BlockCopy(from: ptr, to: pBytes, byteCount: byteCount);
                }
                return bytes;
            }
        }

        /// <summary>
        /// Creates a new managed byte[] from unmanaged memory. The returned value will be protected
        /// by CryptProtectMemory.
        /// </summary>
        public static byte[] ToProtectedManagedByteArray(byte* ptr, int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            fixed (byte* pBytes = bytes)
            {
                try
                {
                    BlockCopy(from: ptr, to: pBytes, byteCount: byteCount);
                    BCryptUtil.ProtectMemoryWithinThisProcess(pBytes, (uint)byteCount);
                }
                catch
                {
                    SecureZeroMemory(pBytes, byteCount);
                    throw;
                }
            }
            return bytes;
        }
    }
}
