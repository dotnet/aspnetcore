// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}
