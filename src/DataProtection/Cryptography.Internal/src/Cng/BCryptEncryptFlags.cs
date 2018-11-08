// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Cryptography.Cng
{
    [Flags]
    internal enum BCryptEncryptFlags
    {
        BCRYPT_BLOCK_PADDING = 0x00000001,
    }
}
