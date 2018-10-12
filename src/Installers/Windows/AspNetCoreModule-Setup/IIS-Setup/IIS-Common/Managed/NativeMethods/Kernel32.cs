// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

// Any reference to Common.cs should also include Kernel32.cs because 
// SafeHandleZeroIsInvalid (from Common.cs) uses CloseHandle (from Kernel32.cs)
namespace Microsoft.Web.Management.PInvoke.Kernel32
{
    internal enum OSProductType : byte
    {
        VER_NT_WORKSTATION = 0x0000001,
        VER_NT_SERVER = 0x0000003,
        VER_NT_DOMAIN_CONTROLLER = 0x0000002,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OSVERSIONINFOEX
    {
        public int dwOSVersionInfoSize;
        public int dwMajorVersion;
        public int dwMinorVersion;
        public int dwBuildNumber;
        public int dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
        public short wServicePackMajor;
        public short wServicePackMinor;
        public short wSuiteMask;
        public OSProductType wProductType;
        public byte wReserved;

        public bool IsServer
        {
            get
            {
                return
                    wProductType == OSProductType.VER_NT_SERVER ||
                    wProductType == OSProductType.VER_NT_DOMAIN_CONTROLLER;
            }
        }

        public bool IsClient
        {
            get
            {
                return
                    wProductType == OSProductType.VER_NT_WORKSTATION;
            }
        }
    }

    internal static class NativeMethods
    {
        public static OSVERSIONINFOEX GetVersionEx()
        {
            OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
            osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));
            if (!GetVersionEx(ref osVersionInfo))
            {
                throw new Win32Exception();
            }

            return osVersionInfo;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        [SuppressMessage("Microsoft.Usage", "CA2205:UseManagedEquivalentsOfWin32Api", Justification = "GetVersionEx returns more information")]
        private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern SafeHandleZeroIsInvalid GetCurrentProcess();

        // WARNING: Vista+ ONLY
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProductInfo(uint dwOSMajorVersion, uint dwOSMinorVersion, uint dwSpMajorVersion, uint spMinorVersion, out uint pdwReturnedProductType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrivateProfileString(string applicationName, string keyName, string stringValue, string fileName);
    }
}
