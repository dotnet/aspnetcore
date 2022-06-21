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

    [CustomTypeMarshaller(typeof(IISConfigurationData), CustomTypeMarshallerKind.Value, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling)]
    public unsafe ref struct Marshaller
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

        private Native _native;

        public Marshaller(IISConfigurationData managed)
        {
            _native.pNativeApplication = managed.pNativeApplication;
            _native.pwzFullApplicationPath = managed.pwzFullApplicationPath is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzFullApplicationPath);
            _native.pwzVirtualApplicationPath = managed.pwzVirtualApplicationPath is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzVirtualApplicationPath);
            _native.fWindowsAuthEnabled = managed.fWindowsAuthEnabled ? 1 : 0;
            _native.fBasicAuthEnabled = managed.fBasicAuthEnabled ? 1 : 0;
            _native.fAnonymousAuthEnable = managed.fAnonymousAuthEnable ? 1 : 0;
            _native.pwzBindings = managed.pwzBindings is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzBindings);
            _native.maxRequestBodySize = managed.maxRequestBodySize;
        }

        public Native ToNativeValue() => _native;

        public void FromNativeValue(Native value) => _native = value;

        public IISConfigurationData ToManaged()
        {
            return new()
            {
                pNativeApplication = _native.pNativeApplication,
                pwzFullApplicationPath = _native.pwzFullApplicationPath == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(_native.pwzFullApplicationPath),
                pwzVirtualApplicationPath = _native.pwzVirtualApplicationPath == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(_native.pwzVirtualApplicationPath),
                fWindowsAuthEnabled = _native.fWindowsAuthEnabled != 0,
                fBasicAuthEnabled = _native.fBasicAuthEnabled != 0,
                fAnonymousAuthEnable = _native.fAnonymousAuthEnable != 0,
                pwzBindings = _native.pwzBindings == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(_native.pwzBindings),
                maxRequestBodySize = _native.maxRequestBodySize
            };
        }

        public void FreeNative()
        {
            if (_native.pwzFullApplicationPath != IntPtr.Zero)
            {
                Marshal.FreeBSTR(_native.pwzFullApplicationPath);
            }
            if (_native.pwzVirtualApplicationPath != IntPtr.Zero)
            {
                Marshal.FreeBSTR(_native.pwzVirtualApplicationPath);
            }
            if (_native.pwzBindings != IntPtr.Zero)
            {
                Marshal.FreeBSTR(_native.pwzBindings);
            }
        }
    }
}
