// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct IISConfigurationData
    {
        public IntPtr pNativeApplication;
        [MarshalAs(UnmanagedType.BStr)]
        public string pwzFullApplicationPath;
        [MarshalAs(UnmanagedType.BStr)]
        public string pwzVirtualApplicationPath;
        public bool fWindowsAuthEnabled;
        public bool fBasicAuthEnabled;
        public bool fAnonymousAuthEnable;
        [MarshalAs(UnmanagedType.BStr)]
        public string pwzBindings;
        public uint maxRequestBodySize;
    }
}
