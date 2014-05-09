// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection
{
    // from bcrypt.h
    [Flags]
    internal enum BCryptAlgorithmFlags
    {
        BCRYPT_ALG_HANDLE_HMAC_FLAG = 0x00000008,
        BCRYPT_CAPI_AES_FLAG = 0x00000010,
        BCRYPT_HASH_REUSABLE_FLAG = 0x00000020,
    }
}
