// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

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
#if DOTNET5_4
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
