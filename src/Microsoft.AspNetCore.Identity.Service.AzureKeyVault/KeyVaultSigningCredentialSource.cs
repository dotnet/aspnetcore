// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service.AzureKeyVault
{
    public class KeyVaultSigningCredentialSource : ISigningCredentialsSource
    {
        private readonly IOptions<KeyVaultSigningCredentialsSourceOptions> _options;

        public KeyVaultSigningCredentialSource(IOptions<KeyVaultSigningCredentialsSourceOptions> options)
        {
            _options = options;
        }

        public async Task<IEnumerable<SigningCredentialsDescriptor>> GetCredentials()
        {
            var options = _options.Value;
            var client = new KeyVaultClient(KeyVaultCallBack, options.ClientHandler);

            var certificateBundle = await client.GetCertificateAsync(options.VaultUri, options.CertificateName);
            var secret = await client.GetSecretAsync(certificateBundle.SecretIdentifier.Identifier);
            var certificate = new X509Certificate2(Base64UrlEncoder.DecodeBytes(secret.Value), string.Empty);
            var signingCredentials = new SigningCredentials(new X509SecurityKey(certificate), CryptographyHelpers.FindAlgorithm(certificate));
            var descriptor = new SigningCredentialsDescriptor(
                signingCredentials,
                CryptographyHelpers.GetAlgorithm(signingCredentials),
                certificateBundle.Attributes.NotBefore.Value.ToUniversalTime(),
                certificateBundle.Attributes.Expires.Value.ToUniversalTime(),
                GetMetadata(signingCredentials));

            return new List<SigningCredentialsDescriptor>() { descriptor };

            IDictionary<string, string> GetMetadata(SigningCredentials credentials)
            {
                var rsaParameters = CryptographyHelpers.GetRSAParameters(credentials);
                return new Dictionary<string, string>
                {
                    [JsonWebKeyParameterNames.E] = Base64UrlEncoder.Encode(rsaParameters.Exponent),
                    [JsonWebKeyParameterNames.N] = Base64UrlEncoder.Encode(rsaParameters.Modulus),
                };
            }

            async Task<string> KeyVaultCallBack(string authority, string resource, string scope)
            {

                var adCredential = new ClientCredential(options.ClientId, options.ClientSecret);
                var authenticationContext = new AuthenticationContext(authority, null);
                var tokenResponse = await authenticationContext.AcquireTokenAsync(resource, adCredential);
                return tokenResponse.AccessToken;
            }
        }
    }
}
