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
// <copyright file="AuthIdentity.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct AuthIdentity
    {
        // see SEC_WINNT_AUTH_IDENTITY_W
        internal string UserName;
        internal int UserNameLength;
        internal string Domain;
        internal int DomainLength;
        internal string Password;
        internal int PasswordLength;
        internal int Flags;

        internal AuthIdentity(string userName, string password, string domain)
        {
            UserName = userName;
            UserNameLength = userName == null ? 0 : userName.Length;
            Password = password;
            PasswordLength = password == null ? 0 : password.Length;
            Domain = domain;
            DomainLength = domain == null ? 0 : domain.Length;
            // Flags are 2 for Unicode and 1 for ANSI. We use 2 on NT and 1 on Win9x.
            Flags = 2;
        }

        public override string ToString()
        {
            return ValidationHelper.ToString(Domain) + "\\" + ValidationHelper.ToString(UserName);
        }
    }
}
