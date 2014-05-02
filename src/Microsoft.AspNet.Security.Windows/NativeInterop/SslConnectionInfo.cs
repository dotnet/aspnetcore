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
// <copyright file="SslConnectionInfo.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    internal class SslConnectionInfo
    {
        public readonly int Protocol;
        public readonly int DataCipherAlg;
        public readonly int DataKeySize;
        public readonly int DataHashAlg;
        public readonly int DataHashKeySize;
        public readonly int KeyExchangeAlg;
        public readonly int KeyExchKeySize;

        internal unsafe SslConnectionInfo(byte[] nativeBuffer)
        {
            fixed (void* voidPtr = nativeBuffer)
            {
                IntPtr unmanagedAddress = new IntPtr(voidPtr);
                Protocol = Marshal.ReadInt32(unmanagedAddress);
                DataCipherAlg = Marshal.ReadInt32(unmanagedAddress, 4);
                DataKeySize = Marshal.ReadInt32(unmanagedAddress, 8);
                DataHashAlg = Marshal.ReadInt32(unmanagedAddress, 12);
                DataHashKeySize = Marshal.ReadInt32(unmanagedAddress, 16);
                KeyExchangeAlg = Marshal.ReadInt32(unmanagedAddress, 20);
                KeyExchKeySize = Marshal.ReadInt32(unmanagedAddress, 24);
            }
        }
    }
}
