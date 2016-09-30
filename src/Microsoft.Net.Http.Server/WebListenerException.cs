// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Http.Server
{
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class WebListenerException : Win32Exception
    {
        internal WebListenerException()
            : base(Marshal.GetLastWin32Error())
        {
        }

        internal WebListenerException(int errorCode)
            : base(errorCode)
        {
        }

        internal WebListenerException(int errorCode, string message)
            : base(errorCode, message)
        {
        }
#if NETSTANDARD1_3
        public int ErrorCode
#else
        // the base class returns the HResult with this property
        // we need the Win32 Error Code, hence the override.
        public override int ErrorCode
#endif
        {
            get
            {
                return NativeErrorCode;
            }
        }
    }
}
