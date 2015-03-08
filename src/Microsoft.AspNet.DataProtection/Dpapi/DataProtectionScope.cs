// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// We only define this type in core CLR since desktop CLR already contains it.
#if DNXCORE50
using System;

namespace System.Security.Cryptography
{
    //
    // Summary:
    //     Specifies the scope of the data protection to be applied by the System.Security.Cryptography.ProtectedData.Protect(System.Byte[],System.Byte[],System.Security.Cryptography.DataProtectionScope)
    //     method.
    internal enum DataProtectionScope
    {
        //
        // Summary:
        //     The protected data is associated with the current user. Only threads running
        //     under the current user context can unprotect the data.
        CurrentUser,
        //
        // Summary:
        //     The protected data is associated with the machine context. Any process running
        //     on the computer can unprotect data. This enumeration value is usually used in
        //     server-specific applications that run on a server where untrusted users are not
        //     allowed access.
        LocalMachine
    }
}
#endif
