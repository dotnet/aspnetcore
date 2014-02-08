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
