// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking
{
    internal class LibuvFunctions
    {
        public LibuvFunctions() { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct uv_buf_t
        {
            private readonly System.IntPtr _field0;
            private readonly System.IntPtr _field1;
            public uv_buf_t(System.IntPtr memory, int len, bool IsWindows) { _field0 = (System.IntPtr)0; _field1 = (System.IntPtr)0; }
        }
    }
}
