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
            var rsa = RSA.Create();
            if (rsa.KeySize < 2048)
            {
                rsa.KeySize = 2048;
                if (rsa.KeySize < 2048 && rsa is RSACryptoServiceProvider)
                {
                    rsa.Dispose();
                    rsa = new RSACryptoServiceProvider(2048);
                }
            }

            SecurityKey key;
            var parameters = rsa.ExportParameters(includePrivateParameters: true);
            rsa.Dispose();

            key = new RsaSecurityKey(parameters);
            key.KeyId = id;
            return key;
        }
    }
}
