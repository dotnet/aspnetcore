// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.AspNetCore.Server.IIS.Core;

[NativeMarshalling(typeof(Marshaller))]
[StructLayout(LayoutKind.Sequential)]
internal struct IISConfigurationData
{
    public IntPtr pNativeApplication;
    public string pwzFullApplicationPath;
    public string pwzVirtualApplicationPath;
    public bool fWindowsAuthEnabled;
    public bool fBasicAuthEnabled;
    public bool fAnonymousAuthEnable;
    public string pwzBindings;
    public uint maxRequestBodySize;

    [CustomMarshaller(typeof(IISConfigurationData), MarshalMode.Default, typeof(Marshaller))]
    public static class Marshaller
    {
        public struct Native
        {
            public IntPtr pNativeApplication;
            public IntPtr pwzFullApplicationPath;
            public IntPtr pwzVirtualApplicationPath;
            public int fWindowsAuthEnabled;
            public int fBasicAuthEnabled;
            public int fAnonymousAuthEnable;
            public IntPtr pwzBindings;
            public uint maxRequestBodySize;
        }

        public static Native ConvertToUnmanaged(IISConfigurationData managed)
        {
            Native native;
            native.pNativeApplication = managed.pNativeApplication;
            native.pwzFullApplicationPath = managed.pwzFullApplicationPath is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzFullApplicationPath);
            native.pwzVirtualApplicationPath = managed.pwzVirtualApplicationPath is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzVirtualApplicationPath);
            native.fWindowsAuthEnabled = managed.fWindowsAuthEnabled ? 1 : 0;
            native.fBasicAuthEnabled = managed.fBasicAuthEnabled ? 1 : 0;
            native.fAnonymousAuthEnable = managed.fAnonymousAuthEnable ? 1 : 0;
            native.pwzBindings = managed.pwzBindings is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzBindings);
            native.maxRequestBodySize = managed.maxRequestBodySize;
            return native;
        }

        public static void Free(Native native)
        {
            if (native.pwzFullApplicationPath != IntPtr.Zero)
            {
                Marshal.FreeBSTR(native.pwzFullApplicationPath);
            }
            if (native.pwzVirtualApplicationPath != IntPtr.Zero)
            {
                Marshal.FreeBSTR(native.pwzVirtualApplicationPath);
            }
            if (native.pwzBindings != IntPtr.Zero)
            {
                Marshal.FreeBSTR(native.pwzBindings);
            }
        }

        public static IISConfigurationData ConvertToManaged(Native native)
        {
            return new()
            {
                pNativeApplication = native.pNativeApplication,
                pwzFullApplicationPath = native.pwzFullApplicationPath == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(native.pwzFullApplicationPath),
                pwzVirtualApplicationPath = native.pwzVirtualApplicationPath == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(native.pwzVirtualApplicationPath),
                fWindowsAuthEnabled = native.fWindowsAuthEnabled != 0,
                fBasicAuthEnabled = native.fBasicAuthEnabled != 0,
                fAnonymousAuthEnable = native.fAnonymousAuthEnable != 0,
                pwzBindings = native.pwzBindings == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(native.pwzBindings),
                maxRequestBodySize = native.maxRequestBodySize
            };
        }
    }
}
