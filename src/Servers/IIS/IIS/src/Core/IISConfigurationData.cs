// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.AspNetCore.Server.IIS.Core;

[NativeMarshalling(typeof(Native))]
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

    [CustomTypeMarshaller(typeof(IISConfigurationData), CustomTypeMarshallerKind.Value, Direction = CustomTypeMarshallerDirection.Ref, Features = CustomTypeMarshallerFeatures.UnmanagedResources | CustomTypeMarshallerFeatures.TwoStageMarshalling)]
    public unsafe ref struct Native
    {
        public IntPtr pNativeApplication;
        public IntPtr pwzFullApplicationPath;
        public IntPtr pwzVirtualApplicationPath;
        public int fWindowsAuthEnabled;
        public int fBasicAuthEnabled;
        public int fAnonymousAuthEnable;
        public IntPtr pwzBindings;
        public uint maxRequestBodySize;

        public Native(IISConfigurationData managed)
        {
            pNativeApplication = managed.pNativeApplication;
            pwzFullApplicationPath = managed.pwzFullApplicationPath is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzFullApplicationPath);
            pwzVirtualApplicationPath = managed.pwzVirtualApplicationPath is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzVirtualApplicationPath);
            fWindowsAuthEnabled = managed.fWindowsAuthEnabled ? 1 : 0;
            fBasicAuthEnabled = managed.fBasicAuthEnabled ? 1 : 0;
            fAnonymousAuthEnable = managed.fAnonymousAuthEnable ? 1 : 0;
            pwzBindings = managed.pwzBindings is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzBindings);
            maxRequestBodySize = managed.maxRequestBodySize;
        }

        public IntPtr ToNativeValue() => (IntPtr)Unsafe.AsPointer(ref pNativeApplication);

        public void FromNativeValue(IntPtr value)
        {
            Debug.Assert(value == ToNativeValue());
        }

        public IISConfigurationData ToManaged()
        {
            return new()
            {
                pNativeApplication = pNativeApplication,
                pwzFullApplicationPath = pwzFullApplicationPath == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(pwzFullApplicationPath),
                pwzVirtualApplicationPath = pwzVirtualApplicationPath == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(pwzVirtualApplicationPath),
                fWindowsAuthEnabled = fWindowsAuthEnabled != 0,
                fBasicAuthEnabled = fBasicAuthEnabled != 0,
                fAnonymousAuthEnable = fAnonymousAuthEnable != 0,
                pwzBindings = pwzBindings == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(pwzBindings),
                maxRequestBodySize = maxRequestBodySize
            };
        }

        public void FreeNative()
        {
            if (pwzFullApplicationPath != IntPtr.Zero)
            {
                Marshal.FreeBSTR(pwzFullApplicationPath);
            }
            if (pwzVirtualApplicationPath != IntPtr.Zero)
            {
                Marshal.FreeBSTR(pwzVirtualApplicationPath);
            }
            if (pwzBindings != IntPtr.Zero)
            {
                Marshal.FreeBSTR(pwzBindings);
            }
        }
    }
}
