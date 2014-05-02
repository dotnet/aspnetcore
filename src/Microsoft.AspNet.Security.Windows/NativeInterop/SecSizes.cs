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

// -----------------------------------------------------------------------
// <copyright file="SecSizes.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.AspNet.Security.Windows
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal class SecSizes
    {
        public readonly int MaxToken;
        public readonly int MaxSignature;
        public readonly int BlockSize;
        public readonly int SecurityTrailer;

        internal unsafe SecSizes(byte[] memory)
        {
            fixed (void* voidPtr = memory)
            {
                IntPtr unmanagedAddress = new IntPtr(voidPtr);
                try
                {
                    MaxToken = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress));
                    MaxSignature = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 4));
                    BlockSize = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 8));
                    SecurityTrailer = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 12));
                }
                catch (OverflowException)
                {
                    GlobalLog.Assert(false, "SecSizes::.ctor", "Negative size.");
                    throw;
                }
            }
        }
        public static readonly int SizeOf = Marshal.SizeOf(typeof(SecSizes));
    }
}
