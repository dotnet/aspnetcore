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
// <copyright file="NegotiationInfoClass.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.AspNet.Security.Windows
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;

    internal class NegotiationInfoClass
    {
        internal const string NTLM = "NTLM";
        internal const string Kerberos = "Kerberos";
        internal const string WDigest = "WDigest";
        internal const string Digest = "Digest";
        internal const string Negotiate = "Negotiate";
        internal string AuthenticationPackage;

        internal NegotiationInfoClass(SafeHandle safeHandle, int negotiationState)
        {
            if (safeHandle.IsInvalid)
            {
                GlobalLog.Print("NegotiationInfoClass::.ctor() the handle is invalid:" + (safeHandle.DangerousGetHandle()).ToString("x"));
                return;
            }
            IntPtr packageInfo = safeHandle.DangerousGetHandle();
            GlobalLog.Print("NegotiationInfoClass::.ctor() packageInfo:" + packageInfo.ToString("x8") + " negotiationState:" + negotiationState.ToString("x8"));

            const int SECPKG_NEGOTIATION_COMPLETE = 0;
            const int SECPKG_NEGOTIATION_OPTIMISTIC = 1;
            // const int SECPKG_NEGOTIATION_IN_PROGRESS     = 2;
            // const int SECPKG_NEGOTIATION_DIRECT          = 3;
            // const int SECPKG_NEGOTIATION_TRY_MULTICRED   = 4;

            if (negotiationState == SECPKG_NEGOTIATION_COMPLETE || negotiationState == SECPKG_NEGOTIATION_OPTIMISTIC)
            {
                IntPtr unmanagedString = Marshal.ReadIntPtr(packageInfo, SecurityPackageInfo.NameOffest);
                string name = null;
                if (unmanagedString != IntPtr.Zero)
                {
                    name = Marshal.PtrToStringUni(unmanagedString);
                }
                GlobalLog.Print("NegotiationInfoClass::.ctor() packageInfo:" + packageInfo.ToString("x8") + " negotiationState:" + negotiationState.ToString("x8") + " name:" + ValidationHelper.ToString(name));

                // an optimization for future string comparisons
                if (string.Compare(name, Kerberos, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = Kerberos;
                }
                else if (string.Compare(name, NTLM, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = NTLM;
                }
                else if (string.Compare(name, WDigest, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = WDigest;
                }
                else
                {
                    AuthenticationPackage = name;
                }
            }
        }
    }
}
