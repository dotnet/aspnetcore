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

// -----------------------------------------------------------------------
// <copyright file="SafeFreeCertContext.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.Windows
{
    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeFreeCertContext : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeFreeCertContext()
            : base(true)
        {
        }

        // This must be ONLY called from this file within a CER.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe void Set(IntPtr value)
        {
            this.handle = value;
        }

        protected override bool ReleaseHandle()
        {
            UnsafeNclNativeMethods.SafeNetHandles.CertFreeCertificateContext(handle);
            return true;
        }
    }
}
