// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.AspNetCore.Server.IIS.Core;

[NativeMarshalling(typeof(Marshaller))]
[StructLayout(LayoutKind.Sequential)]
internal struct IISConfigurationData : IIISEnvironmentFeature
{
    public IntPtr pNativeApplication;
    public string pwzFullApplicationPath;
    public string pwzVirtualApplicationPath;
    public bool fWindowsAuthEnabled;
    public bool fBasicAuthEnabled;
    public bool fAnonymousAuthEnable;
    public string pwzBindings;
    public uint maxRequestBodySize;
    public string pwzApplicationId;
    public string pwzSiteName;
    public uint siteId;
    public string pwzAppPoolId;
    public string pwzAppPoolConfig;
    public Version version;

    Version IIISEnvironmentFeature.IISVersion => version;

    string IIISEnvironmentFeature.AppPoolId => pwzAppPoolId;

    string IIISEnvironmentFeature.AppPoolConfig => pwzAppPoolConfig;

    string IIISEnvironmentFeature.ApplicationId => pwzApplicationId;

    string IIISEnvironmentFeature.SiteName => pwzSiteName;

    uint IIISEnvironmentFeature.SiteId => siteId;

    string IIISEnvironmentFeature.ApplicationPath => pwzFullApplicationPath;

    string IIISEnvironmentFeature.ApplicationVirtualPath => pwzVirtualApplicationPath;

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
            public IntPtr pwzApplicationId;
            public IntPtr pwzSiteName;
            public uint siteId;
            public IntPtr pwzAppPoolId;
            public IntPtr pwzAppPoolConfig;
            public uint version;
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
            native.pwzApplicationId = managed.pwzApplicationId is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzApplicationId);
            native.pwzSiteName = managed.pwzSiteName is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzApplicationId);
            native.siteId = managed.siteId;
            native.pwzAppPoolId = managed.pwzAppPoolId is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzAppPoolId);
            native.pwzAppPoolConfig = managed.pwzAppPoolConfig is null ? IntPtr.Zero : Marshal.StringToBSTR(managed.pwzAppPoolConfig);
            native.version = ConvertFromVersion(managed.version);

            return native;
        }

        private static Version ConvertToVersion(uint dwVersion) => new((int)(dwVersion >> 16), (int)(dwVersion & 0xffff));

        private static uint ConvertFromVersion(Version version) => ((uint)version.Major << 16) | ((uint)version.Minor);

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
            if (native.pwzApplicationId != IntPtr.Zero)
            {
                Marshal.FreeBSTR(native.pwzApplicationId);
            }
            if (native.pwzSiteName != IntPtr.Zero)
            {
                Marshal.FreeBSTR(native.pwzSiteName);
            }
            if (native.pwzAppPoolId != IntPtr.Zero)
            {
                Marshal.FreeBSTR(native.pwzAppPoolId);
            }
            if (native.pwzAppPoolConfig != IntPtr.Zero)
            {
                Marshal.FreeBSTR(native.pwzAppPoolConfig);
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
                maxRequestBodySize = native.maxRequestBodySize,
                pwzApplicationId = native.pwzApplicationId == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(native.pwzApplicationId),
                pwzSiteName = native.pwzSiteName == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(native.pwzSiteName),
                siteId = native.siteId,
                pwzAppPoolConfig = native.pwzAppPoolConfig == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(native.pwzAppPoolConfig),
                pwzAppPoolId = native.pwzAppPoolId == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(native.pwzAppPoolId),
                version = ConvertToVersion(native.version),
            };
        }
    }
}
