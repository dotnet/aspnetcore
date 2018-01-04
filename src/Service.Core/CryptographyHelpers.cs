// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    internal static class CryptographyHelpers
    {
        private static readonly RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

        public static string FindAlgorithm(X509Certificate2 certificate)
        {
            var rsapk = certificate.GetRSAPublicKey();
            if (rsapk != null)
            {
                return "RS256";
            }
            else
            {
                throw new InvalidOperationException("Algorithm not supported.");
            }
        }

        public static void ValidateRsaKeyLength(X509Certificate2 certificate)
        {
            var rsa = certificate.GetRSAPublicKey();
            if (rsa == null)
            {
                throw new InvalidOperationException("Algorithm not supported.");
            }
            ValidateRsaKeyLength(rsa);
        }

        public static void ValidateRsaKeyLength(RSA rsa)
        {
            if (rsa.KeySize < 2048)
            {
                throw new InvalidOperationException("The RSA key must be at least 2048 bits long.");
            }

            return;
        }

        public static SHA256 CreateSHA256()
        {
            SHA256 sha256 = null;
            try
            {
                sha256 = SHA256.Create();
                return sha256;
            }
            // SHA256.Create is documented to throw this exception on FIPS compliant machines.
            // See: https://msdn.microsoft.com/enus/library/z08hz7ad%28v=vs.110%29.aspx?f=255&MSPPError=2147217396
            catch (System.Reflection.TargetInvocationException)
            {
                // Fallback to a FIPS compliant SHA256 algorithm.
                sha256 = new SHA256CryptoServiceProvider();
            }

            return sha256;
        }

        public static RSAParameters GetRSAParameters(SigningCredentials credentials)
        {
            RSA algorithm = null;
            if (credentials.Key is X509SecurityKey x509SecurityKey)
            {
                algorithm = x509SecurityKey.PublicKey as RSA;
            }

            if (credentials.Key is RsaSecurityKey rsaSecurityKey)
            {
                algorithm = rsaSecurityKey.Rsa;

                if (algorithm == null)
                {
                    var rsa = RSA.Create();
                    rsa.ImportParameters(rsaSecurityKey.Parameters);
                    algorithm = rsa;
                }
            }

            var parameters = algorithm.ExportParameters(includePrivateParameters: false);
            return parameters;
        }

        public static string GetAlgorithm(SigningCredentials credentials)
        {
            RSA algorithm = null;
            if (credentials.Key is X509SecurityKey x509SecurityKey && x509SecurityKey.PublicKey is RSA)
            {
                return JsonWebAlgorithmsKeyTypes.RSA;
            }

            var rsaSecurityKey = credentials.Key as RsaSecurityKey;
            // Check that the key has either an Asymetric Algorithm assigned or that at least
            // one of the RSA parameters are initialized to consider the key "valid".
            if (rsaSecurityKey != null &&
                (rsaSecurityKey.Rsa != null || rsaSecurityKey.Parameters.Modulus != null))
            {
                return JsonWebAlgorithmsKeyTypes.RSA;
            }

            if (algorithm != null)
            {
                return JsonWebAlgorithmsKeyTypes.RSA;
            }

            throw new NotSupportedException();
        }

        internal static string GenerateHighEntropyValue(int byteLength)
        {
            var bytes = new byte[byteLength];
            randomNumberGenerator.GetBytes(bytes);
            return Base64UrlEncoder.Encode(bytes);
        }
    }
}
