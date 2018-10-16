// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Cryptography
{
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa381414(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DATA_BLOB
    {
        public uint cbData;
        public byte* pbData;
    }
}
