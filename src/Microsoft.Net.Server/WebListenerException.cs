//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Server
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
#if NET45
        // the base class returns the HResult with this property
        // we need the Win32 Error Code, hence the override.
        public override int ErrorCode
#else
        public int ErrorCode
#endif
        {
            get
            {
                return NativeErrorCode;
            }
        }
    }
}
