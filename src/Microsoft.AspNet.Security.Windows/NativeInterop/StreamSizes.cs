// -----------------------------------------------------------------------
// <copyright file="StreamSizes.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    internal class StreamSizes
    {
        public int header;
        public int trailer;
        public int maximumMessage;
        public int buffersCount;
        public int blockSize;

        internal unsafe StreamSizes(byte[] memory)
        {
            fixed (void* voidPtr = memory)
            {
                IntPtr unmanagedAddress = new IntPtr(voidPtr);
                try
                {
                    header = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress));
                    trailer = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 4));
                    maximumMessage = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 8));
                    buffersCount = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 12));
                    blockSize = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 16));
                }
                catch (OverflowException)
                {
                    GlobalLog.Assert(false, "StreamSizes::.ctor", "Negative size.");
                    throw;
                }
            }
        }
        public static readonly int SizeOf = Marshal.SizeOf(typeof(StreamSizes));
    }
}
