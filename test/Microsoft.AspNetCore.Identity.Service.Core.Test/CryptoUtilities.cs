// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    internal static class CryptoUtilities
    {
        internal static SecurityKey CreateTestKey(string id = "Test")
        {
            using (var rsa = RSA.Create(2048))
            {
                SecurityKey key;
                var parameters = rsa.ExportParameters(includePrivateParameters: true);
                key = new RsaSecurityKey(parameters);
                key.KeyId = id;
                return key;
            }
        }
    }
}
