// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class HttpSysException : Win32Exception
    {
        internal HttpSysException()
            : base(Marshal.GetLastWin32Error())
        {
        }

        internal HttpSysException(int errorCode)
            : base(errorCode)
        {
        }

        internal HttpSysException(int errorCode, string message)
            : base(errorCode, message)
        {
        }

        // the base class returns the HResult with this property
        // we need the Win32 Error Code, hence the override.
        public override int ErrorCode
        {
            get
            {
                return NativeErrorCode;
            }
        }
    }
}
