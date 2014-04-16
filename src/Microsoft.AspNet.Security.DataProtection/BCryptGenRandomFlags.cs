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
