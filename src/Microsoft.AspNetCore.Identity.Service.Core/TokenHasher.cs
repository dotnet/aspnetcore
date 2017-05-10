// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenHasher : ITokenHasher
    {
        public string HashToken(string token, string hashingAlgorithm)
        {
            var algorithm = GetAlgorithm(hashingAlgorithm);

            var bytes = Encoding.ASCII.GetBytes(token);
            var hashed = algorithm.ComputeHash(bytes);
            var result = Base64UrlEncoder.Encode(hashed, 0, hashed.Length / 2);

            return result;
        }

        private HashAlgorithm GetAlgorithm(string hashingAlgorithm)
        {
            switch (hashingAlgorithm)
            {
                case "RS256":
                    return CryptographyHelpers.CreateSHA256();
                default:
                    throw new InvalidOperationException($"Unsupported hashing algorithm '{hashingAlgorithm}'");
            }
        }
    }
}
