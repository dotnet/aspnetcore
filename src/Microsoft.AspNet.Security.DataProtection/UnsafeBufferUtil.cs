// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;

#if !ASPNETCORE50
using System.Runtime.ConstrainedExecution;
#endif

namespace Microsoft.AspNet.Security.DataProtection
{
    internal unsafe static class UnsafeBufferUtil
    {
        private static readonly byte[] _emptyArray = new byte[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void BlockCopy(void* from, void* to, int byteCount)
        {
            BlockCopy(from, to, checked((uint)byteCount)); // will be checked before invoking the delegate
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void BlockCopy(void* from, void* to, uint byteCount)
        {
            if (byteCount != 0)
            {
                BlockCopyImpl((byte*)from, (byte*)to, byteCount);
            }
        }

#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void BlockCopy(LocalAllocHandle from, void* to, uint byteCount)
        {
            bool refAdded = false;
            try
            {
                from.DangerousAddRef(ref refAdded);
                BlockCopy((void*)from.DangerousGetHandle(), to, byteCount);
            }
            finally
            {
                if (refAdded)
                {
                    from.DangerousRelease();
                }
            }
        }

#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void BlockCopy(byte* from, LocalAllocHandle to, uint byteCount)
        {
            bool refAdded = false;
            try
            {
                to.DangerousAddRef(ref refAdded);
                BlockCopy(from, (void*)to.DangerousGetHandle(), byteCount);
            }
            finally
            {
                if (refAdded)
                {
                    to.DangerousRelease();
                }
            }
        }

#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        public static void BlockCopy(LocalAllocHandle from, LocalAllocHandle to, IntPtr length)
        {
            if (length == IntPtr.Zero)
            {
                return;
            }

            bool fromRefAdded = false;
            bool toRefAdded = false;
            try
            {
                from.DangerousAddRef(ref fromRefAdded);
                to.DangerousAddRef(ref toRefAdded);
                if (sizeof(IntPtr) == 4)
                {
                    BlockCopyImpl(from: (byte*)from.DangerousGetHandle(), to: (byte*)to.DangerousGetHandle(), byteCount: (uint)length.ToInt32());
                } else
                {
                    BlockCopyImpl(from: (byte*)from.DangerousGetHandle(), to: (byte*)to.DangerousGetHandle(), byteCount: (ulong)length.ToInt64());
                }
            }
            finally
            {
                if (fromRefAdded)
                {
                    from.DangerousRelease();
                }
                if (toRefAdded)
                {
                    to.DangerousRelease();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlockCopyImpl(byte* from, byte* to, uint byteCount)
        {
#if ASPNETCORE50
            Buffer.MemoryCopy(from, to, (ulong)byteCount, (ulong)byteCount);
#else
            while (byteCount-- != 0) {
                to[byteCount] = from[byteCount];
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlockCopyImpl(byte* from, byte* to, ulong byteCount)
        {
#if ASPNETCORE50
            Buffer.MemoryCopy(from, to, byteCount, byteCount);
#else
            while (byteCount-- != 0) {
                to[byteCount] = from[byteCount];
            }
#endif
        }

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void SecureZeroMemory(byte* buffer, int byteCount)
        {
            SecureZeroMemory(buffer, checked((uint)byteCount));
        }

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void SecureZeroMemory(byte* buffer, uint byteCount)
        {
            if (byteCount != 0)
            {
                do
                {
                    buffer[--byteCount] = 0;
                } while (byteCount != 0);

                // Volatile to make sure the zero-writes don't get optimized away
                Volatile.Write(ref *buffer, 0);
            }
        }

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void SecureZeroMemory(byte* buffer, ulong byteCount)
        {
            if (byteCount != 0)
            {
                do
                {
                    buffer[--byteCount] = 0;
                } while (byteCount != 0);

                // Volatile to make sure the zero-writes don't get optimized away
                Volatile.Write(ref *buffer, 0);
            }
        }

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static void SecureZeroMemory(byte* buffer, IntPtr length)
        {
            if (sizeof(IntPtr) == 4)
            {
                SecureZeroMemory(buffer, (uint)length.ToInt32());
            }
            else
            {
                SecureZeroMemory(buffer, (ulong)length.ToInt64());
            }
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
    }
}
