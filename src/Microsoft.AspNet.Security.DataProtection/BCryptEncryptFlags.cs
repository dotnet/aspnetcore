using System;

namespace Microsoft.AspNet.Security.DataProtection
{
    // from bcrypt.h
    [Flags]
    internal enum BCryptEncryptFlags
    {
        BCRYPT_BLOCK_PADDING = 0x00000001,
    }
}
