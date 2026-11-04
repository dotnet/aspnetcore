// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Cryptography.Cng;

[Flags]
internal enum BCryptEncryptFlags
{
    BCRYPT_BLOCK_PADDING = 0x00000001,
}
