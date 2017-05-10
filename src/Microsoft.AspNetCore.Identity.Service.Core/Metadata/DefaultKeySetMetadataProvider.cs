// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service.Metadata
{
    public class DefaultKeySetMetadataProvider : IKeySetMetadataProvider
    {
        private readonly ISigningCredentialsPolicyProvider _provider;

        public DefaultKeySetMetadataProvider(ISigningCredentialsPolicyProvider provider)
        {
            _provider = provider;
        }

        public async Task<JsonWebKeySet> GetKeysAsync()
        {
            var keySet = new JsonWebKeySet();
            var credentials = await _provider.GetAllCredentialsAsync();
            foreach (var key in credentials)
            {
                keySet.Keys.Add(CreateJsonWebKey(key));
            }
            return keySet;
        }

        private JsonWebKey CreateJsonWebKey(SigningCredentialsDescriptor descriptor)
        {
            var jsonWebKey = new JsonWebKey
            {
                Kid = descriptor.Id,
                Use = JsonWebKeyUseNames.Sig,
                Kty = descriptor.Algorithm
            };

            if (!descriptor.Algorithm.Equals(JsonWebAlgorithmsKeyTypes.RSA))
            {
                throw new NotSupportedException();
            }
            if (!descriptor.Metadata.TryGetValue(JsonWebKeyParameterNames.E, out var exponent))
            {
                throw new InvalidOperationException($"Missing '{JsonWebKeyParameterNames.E}' from metadata");
            }
            if (!descriptor.Metadata.TryGetValue(JsonWebKeyParameterNames.N, out var modulus))
            {
                throw new InvalidOperationException($"Missing '{JsonWebKeyParameterNames.N}' from metadata");
            }

            jsonWebKey.E = exponent;
            jsonWebKey.N = modulus;

            return jsonWebKey;
        }

        private static RSAParameters GetRSAParameters(SigningCredentials credentials)
        {
            RSA algorithm = null;
            var x509SecurityKey = credentials.Key as X509SecurityKey;
            if (x509SecurityKey != null)
            {
                algorithm = x509SecurityKey.PublicKey as RSA;
            }

            var rsaSecurityKey = credentials.Key as RsaSecurityKey;
            if (rsaSecurityKey != null)
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
    }
}
