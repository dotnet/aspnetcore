// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

// Any reference to Common.cs should also include Kernel32.cs because 
// SafeHandleZeroIsInvalid (from Common.cs) uses CloseHandle (from Kernel32.cs)
namespace Microsoft.Web.Management.PInvoke
{
    internal enum Win32ErrorCode
    {
        ERROR_FILE_NOT_FOUND = 2,
        ERROR_PATH_NOT_FOUND = 3,
        ERROR_ACCESS_DENIED = 5,
        ERROR_INVALID_HANDLE = 6,
        ERROR_INVALID_DRIVE = 15,
        ERROR_NO_MORE_FILES = 18,
        ERROR_NOT_READY = 21,
        ERROR_SHARING_VIOLATION = 32,
        ERROR_FILE_EXISTS = 80,
        ERROR_INVALID_PARAMETER = 87,       //  0x57
        ERROR_INVALID_NAME = 123,           //  0x7b
        ERROR_BAD_PATHNAME = 161,
        ERROR_ALREADY_EXISTS = 183,
        ERROR_FILENAME_EXCED_RANGE = 206,
        ERROR_OPERATION_ABORTED = 995,
        ELEMENT_NOT_FOUND = 0x490
    }

    internal static class Extension
    {
        public static int AsHRESULT(Win32ErrorCode errorCode)
        {
            return (int)((uint)errorCode | 0x80070000);
        }
    }

    internal class SafeHandleZeroIsInvalid : SafeHandle
    {
        public SafeHandleZeroIsInvalid()
            : base(IntPtr.Zero, true)
        {
        }

        public SafeHandleZeroIsInvalid(IntPtr newHandle)
            : base(IntPtr.Zero, true)
        {
            this.SetHandle(newHandle);
        }

        public override bool IsInvalid
        {
            get
            {
                return this.handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            return Kernel32.NativeMethods.CloseHandle(this.handle);
        }
    }

    internal class HGlobalBuffer : SafeHandle
    {
        private int _size;

        public HGlobalBuffer(int size)
            : base(IntPtr.Zero, true)
        {
            _size = size;
            this.handle = Marshal.AllocHGlobal(size);
        }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                Marshal.FreeHGlobal(this.handle);
                this.handle = IntPtr.Zero;
            }

            return true;
        }

        public override bool IsInvalid
        {
            get
            {
                return this.handle == IntPtr.Zero;
            }
        }

        public T GetCopyAs<T>()
        {
            return (T)Marshal.PtrToStructure(this.handle, typeof(T));
        }

        public int Size
        {
            get
            {
                return _size;
            }
        }

        public static readonly HGlobalBuffer NULL = new HGlobalBuffer(0);
    }
}
