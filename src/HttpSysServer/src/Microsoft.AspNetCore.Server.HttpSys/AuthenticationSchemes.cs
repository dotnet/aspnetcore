// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    // REVIEW: this appears to be very similar to System.Net.AuthenticationSchemes
    [Flags]
    public enum AuthenticationSchemes
    {
        None = 0x0,
        Basic = 0x1,
        // Digest = 0x2, // TODO: Verify this is no longer supported by Http.Sys
        NTLM = 0x4,
        Negotiate = 0x8,
        Kerberos = 0x10
    }
}
