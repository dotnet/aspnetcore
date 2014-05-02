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
// <copyright file="AuthTypes.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Microsoft.AspNet.Security.Windows
{
    /// <summary>
    /// Types of Windows Authentication supported.
    /// </summary>
    [Flags]
    public enum AuthTypes
    {
        /// <summary>
        /// Default
        /// </summary>
        None = 0,

        /// <summary>
        /// Digest authentication using Windows credentials
        /// </summary>
        Digest = 1,

        /// <summary>
        /// Negotiates Kerberos or NTLM
        /// </summary>
        Negotiate = 2,

        /// <summary>
        /// NTLM Windows authentication
        /// </summary>
        Ntlm = 4,
    }
}
