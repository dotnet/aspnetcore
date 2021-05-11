// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// Specifies protocols for authentication.
    /// </summary>
    [Flags]
    public enum AuthenticationSchemes
    {
        /// <summary>
        /// No authentication is enabled. This should only be used when HttpSysOptions.Authentication.AllowAnonymous is enabled (see <see cref="AuthenticationManager.AllowAnonymous"/>).
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Specifies basic authentication.
        /// </summary>
        Basic = 0x1,


        // Digest = 0x2, // TODO: Verify this is no longer supported by Http.Sys

        /// <summary>
        /// Specifies NTLM authentication.
        /// </summary>
        NTLM = 0x4,

        /// <summary>
        /// Negotiates with the client to determine the authentication scheme. If both client and server support Kerberos, it is used;
        /// otherwise, NTLM is used.
        /// </summary>
        Negotiate = 0x8,

        /// <summary>
        /// Specifies Kerberos authentication.
        /// </summary>
        Kerberos = 0x10
    }
}
