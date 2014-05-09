// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection
{
    // from bcrypt.h
    [Flags]
    internal enum BCryptGenRandomFlags
    {
        BCRYPT_RNG_USE_ENTROPY_IN_BUFFER = 0x00000001,
        BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002,
    }
}
