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
// <copyright file="SafeFreeCredentials.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    internal sealed class SafeFreeCredentials : SafeHandle
    {
        internal SSPIHandle _handle;    // should be always used as by ref in PINvokes parameters

        private SafeFreeCredentials()
            : base(IntPtr.Zero, true)
        {
            _handle = new SSPIHandle();
        }

        public override bool IsInvalid
        {
            get { return IsClosed || _handle.IsZero; }
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNclNativeMethods.SafeNetHandles.FreeCredentialsHandle(ref _handle) == 0;
        }

        public static unsafe int AcquireCredentialsHandle(string package, CredentialUse intent, ref AuthIdentity authdata, 
            out SafeFreeCredentials outCredential)
        {
            GlobalLog.Print("SafeFreeCredentials::AcquireCredentialsHandle#1("
                            + package + ", "
                            + intent + ", "
                            + authdata + ")");

            int errorCode = -1;
            long timeStamp;

            outCredential = new SafeFreeCredentials();

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                errorCode = UnsafeNclNativeMethods.SafeNetHandles.AcquireCredentialsHandleW(
                                                        null,
                                                        package,
                                                        (int)intent,
                                                        null,
                                                        ref authdata,
                                                        null,
                                                        null,
                                                        ref outCredential._handle,
                                                        out timeStamp);
            }

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }
            return errorCode;
        }

        public static unsafe int AcquireDefaultCredential(string package, CredentialUse intent, out SafeFreeCredentials outCredential)
        {
            GlobalLog.Print("SafeFreeCredentials::AcquireDefaultCredential("
                            + package + ", "
                            + intent + ")");

            int errorCode = -1;
            long timeStamp;

            outCredential = new SafeFreeCredentials();

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                errorCode = UnsafeNclNativeMethods.SafeNetHandles.AcquireCredentialsHandleW(
                                                    null,
                                                    package,
                                                    (int)intent,
                                                    null,
                                                    IntPtr.Zero,
                                                    null,
                                                    null,
                                                    ref outCredential._handle,
                                                    out timeStamp);
            }

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }
            return errorCode;
        }

        // This overload is only called on Win7+ where SspiEncodeStringsAsAuthIdentity() was used to
        // create the authData blob.
        public static unsafe int AcquireCredentialsHandle(
                                                    string package,
                                                    CredentialUse intent,
                                                    ref SafeSspiAuthDataHandle authdata,
                                                    out SafeFreeCredentials outCredential)
        {
            int errorCode = -1;
            long timeStamp;

            outCredential = new SafeFreeCredentials();

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                errorCode = UnsafeNclNativeMethods.SafeNetHandles.AcquireCredentialsHandleW(
                                                       null,
                                                       package,
                                                       (int)intent,
                                                       null,
                                                       authdata,
                                                       null,
                                                       null,
                                                       ref outCredential._handle,
                                                       out timeStamp);
            }

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }
            return errorCode;
        }

        public static unsafe int AcquireCredentialsHandle(string package, CredentialUse intent, ref SecureCredential authdata, 
            out SafeFreeCredentials outCredential)
        {
            GlobalLog.Print("SafeFreeCredentials::AcquireCredentialsHandle#2("
                            + package + ", "
                            + intent + ", "
                            + authdata + ")");

            int errorCode = -1;
            long timeStamp;

            // If there is a certificate, wrap it into an array.
            // Not threadsafe.
            IntPtr copiedPtr = authdata.certContextArray;
            try
            {
                IntPtr certArrayPtr = new IntPtr(&copiedPtr);
                if (copiedPtr != IntPtr.Zero)
                {
                    authdata.certContextArray = certArrayPtr;
                }

                outCredential = new SafeFreeCredentials();

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    errorCode = UnsafeNclNativeMethods.SafeNetHandles.AcquireCredentialsHandleW(
                                                        null,
                                                        package,
                                                        (int)intent,
                                                        null,
                                                        ref authdata,
                                                        null,
                                                        null,
                                                        ref outCredential._handle,
                                                        out timeStamp);
                }
            }
            finally
            {
                authdata.certContextArray = copiedPtr;
            }

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }
            return errorCode;
        }
    }
}
