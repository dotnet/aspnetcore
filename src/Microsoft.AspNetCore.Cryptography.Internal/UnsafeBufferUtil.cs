// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography
{
    internal unsafe static class UnsafeBufferUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static void BlockCopy(void* from, void* to, int byteCount)
        {
            BlockCopy(from, to, checked((uint)byteCount)); // will be checked before invoking the delegate
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static void BlockCopy(void* from, void* to, uint byteCount)
        {
            if (byteCount != 0)
            {
                BlockCopyCore((byte*)from, (byte*)to, byteCount);
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
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

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static void BlockCopy(void* from, LocalAllocHandle to, uint byteCount)
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

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
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
                    BlockCopyCore(from: (byte*)from.DangerousGetHandle(), to: (byte*)to.DangerousGetHandle(), byteCount: (uint)length.ToInt32());
                }
                else
                {
                    BlockCopyCore(from: (byte*)from.DangerousGetHandle(), to: (byte*)to.DangerousGetHandle(), byteCount: (ulong)length.ToInt64());
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
        private static void BlockCopyCore(byte* from, byte* to, uint byteCount)
        {
            Buffer.MemoryCopy(from, to, (ulong)byteCount, (ulong)byteCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlockCopyCore(byte* from, byte* to, ulong byteCount)
        {
            Buffer.MemoryCopy(from, to, byteCount, byteCount);
        }

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static void SecureZeroMemory(byte* buffer, int byteCount)
        {
            SecureZeroMemory(buffer, checked((uint)byteCount));
        }

        /// <summary>
        /// Securely clears a memory buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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
    }
}
