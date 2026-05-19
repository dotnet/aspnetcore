// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Cryptography.Cng;

// from bcrypt.h
[Flags]
internal enum BCryptGenRandomFlags
{
    BCRYPT_RNG_USE_ENTROPY_IN_BUFFER = 0x00000001,
    BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002,
}
