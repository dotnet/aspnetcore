// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.CodeAnalysis.CommandLine
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct STARTUPINFO
    {
        internal Int32 cb;
        internal string lpReserved;
        internal string lpDesktop;
        internal string lpTitle;
        internal Int32 dwX;
        internal Int32 dwY;
        internal Int32 dwXSize;
        internal Int32 dwYSize;
        internal Int32 dwXCountChars;
        internal Int32 dwYCountChars;
        internal Int32 dwFillAttribute;
        internal Int32 dwFlags;
        internal Int16 wShowWindow;
        internal Int16 cbReserved2;
        internal IntPtr lpReserved2;
        internal IntPtr hStdInput;
        internal IntPtr hStdOutput;
        internal IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    /// <summary>
    /// Interop methods.
    /// </summary>
    internal static class NativeMethods
    {
        #region Constants

        internal static readonly IntPtr NullPtr = IntPtr.Zero;
        internal static readonly IntPtr InvalidIntPtr = new IntPtr((int)-1);

        internal const uint NORMAL_PRIORITY_CLASS = 0x0020;
        internal const uint CREATE_NO_WINDOW = 0x08000000;
        internal const Int32 STARTF_USESTDHANDLES = 0x00000100;
        internal const int ERROR_SUCCESS = 0;

        #endregion

        //------------------------------------------------------------------------------
        // CloseHandle
        //------------------------------------------------------------------------------
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        //------------------------------------------------------------------------------
        // CreateProcess
        //------------------------------------------------------------------------------
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcess
        (
            string lpApplicationName,
            [In, Out]StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            [In, MarshalAs(UnmanagedType.Bool)]
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetCommandLine();
    }
}