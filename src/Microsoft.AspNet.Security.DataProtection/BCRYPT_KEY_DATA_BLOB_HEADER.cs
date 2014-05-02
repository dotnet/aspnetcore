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
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.DataProtection
{
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375524(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct BCRYPT_KEY_DATA_BLOB_HEADER
    {
        // from bcrypt.h
        private const uint BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b; //Key Data Blob Magic (KDBM)
        private const uint BCRYPT_KEY_DATA_BLOB_VERSION1 = 0x1;

        public uint dwMagic;
        public uint dwVersion;
        public uint cbKeyData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize(ref BCRYPT_KEY_DATA_BLOB_HEADER pHeader)
        {
            pHeader.dwMagic = BCRYPT_KEY_DATA_BLOB_MAGIC;
            pHeader.dwVersion = BCRYPT_KEY_DATA_BLOB_VERSION1;
        }
    }
}
