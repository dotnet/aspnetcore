// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNet.Cryptography;

namespace Microsoft.AspNet.DataProtection.Managed
{
    internal static class HashAlgorithmExtensions
    {
        public static int GetDigestSizeInBytes(this HashAlgorithm hashAlgorithm)
        {
            var hashSizeInBits = hashAlgorithm.HashSize;
            CryptoUtil.Assert(hashSizeInBits >= 0 && hashSizeInBits % 8 == 0, "hashSizeInBits >= 0 && hashSizeInBits % 8 == 0");
            return hashSizeInBits / 8;
        }
    }
}
