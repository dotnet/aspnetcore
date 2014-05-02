// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;

namespace Microsoft.AspNet.Security.DataProtection
{
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
}
