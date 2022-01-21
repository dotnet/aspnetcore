// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cryptography.Cng;

// from bcrypt.h
internal enum BCryptKeyDerivationBufferType
{
    KDF_HASH_ALGORITHM = 0x0,
    KDF_SECRET_PREPEND = 0x1,
    KDF_SECRET_APPEND = 0x2,
    KDF_HMAC_KEY = 0x3,
    KDF_TLS_PRF_LABEL = 0x4,
    KDF_TLS_PRF_SEED = 0x5,
    KDF_SECRET_HANDLE = 0x6,
    KDF_TLS_PRF_PROTOCOL = 0x7,
    KDF_ALGORITHMID = 0x8,
    KDF_PARTYUINFO = 0x9,
    KDF_PARTYVINFO = 0xA,
    KDF_SUPPPUBINFO = 0xB,
    KDF_SUPPPRIVINFO = 0xC,
    KDF_LABEL = 0xD,
    KDF_CONTEXT = 0xE,
    KDF_SALT = 0xF,
    KDF_ITERATION_COUNT = 0x10,
}
