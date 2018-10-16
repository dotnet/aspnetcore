// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Cryptography.Cng
{
    [Flags]
    internal enum NCryptEncryptFlags
    {
        NCRYPT_NO_PADDING_FLAG = 0x00000001,
        NCRYPT_PAD_PKCS1_FLAG = 0x00000002,
        NCRYPT_PAD_OAEP_FLAG = 0x00000004,
        NCRYPT_PAD_PSS_FLAG = 0x00000008,
        NCRYPT_SILENT_FLAG = 0x00000040,
    }
}
