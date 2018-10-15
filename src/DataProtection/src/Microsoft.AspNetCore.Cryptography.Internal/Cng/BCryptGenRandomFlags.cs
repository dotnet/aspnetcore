// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Cryptography.Cng
{
    // from bcrypt.h
    [Flags]
    internal enum BCryptGenRandomFlags
    {
        BCRYPT_RNG_USE_ENTROPY_IN_BUFFER = 0x00000001,
        BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002,
    }
}
