// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Cryptography.Cng;

[Flags]
internal enum NCryptEncryptFlags
{
    NCRYPT_NO_PADDING_FLAG = 0x00000001,
    NCRYPT_PAD_PKCS1_FLAG = 0x00000002,
    NCRYPT_PAD_OAEP_FLAG = 0x00000004,
    NCRYPT_PAD_PSS_FLAG = 0x00000008,
    NCRYPT_SILENT_FLAG = 0x00000040,
}
