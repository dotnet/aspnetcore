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