//------------------------------------------------------------------------------
// <copyright file="HttpListenerException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Server.WebListener
{
#if NET45
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
#endif
    internal class WebListenerException : Win32Exception
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

        public override int ErrorCode
        {
            // the base class returns the HResult with this property
            // we need the Win32 Error Code, hence the override.

            get
            {
                return NativeErrorCode;
            }
        }
    }
}
