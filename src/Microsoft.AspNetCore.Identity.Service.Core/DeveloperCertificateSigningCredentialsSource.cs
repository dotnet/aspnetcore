// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DeveloperCertificateSigningCredentialsSource : ISigningCredentialsSource
    {
        private readonly IHostingEnvironment _environment;
        private readonly ITimeStampManager _timeStampManager;

        public DeveloperCertificateSigningCredentialsSource(
            IHostingEnvironment environment,
            ITimeStampManager timeStampManager)
        {
            _environment = environment;
            _timeStampManager = timeStampManager;
        }

        public Task<IEnumerable<SigningCredentialsDescriptor>> GetCredentials()
        {
            if (!_environment.IsDevelopment())
            {
                return Task.FromResult(Enumerable.Empty<SigningCredentialsDescriptor>());
            }

            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var cert = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, "CN=Identity.Development", validOnly: false);
                var valid = cert.OfType<X509Certificate2>().FirstOrDefault(c => _timeStampManager.IsValidPeriod(c.NotBefore, c.NotAfter));
                store.Close();

                if (valid != null)
                {
                    return Task.FromResult<IEnumerable<SigningCredentialsDescriptor>>(new[] { CreateDescriptor(valid) });
                }
                else
                {
                    return Task.FromResult(Enumerable.Empty<SigningCredentialsDescriptor>());
                }
            }
        }

        private SigningCredentialsDescriptor CreateDescriptor(X509Certificate2 certificate)
        {
            CryptographyHelpers.ValidateRsaKeyLength(certificate);
            var credentials = new SigningCredentials(new X509SecurityKey(certificate), CryptographyHelpers.FindAlgorithm(certificate));
            return new SigningCredentialsDescriptor(
                credentials,
                CryptographyHelpers.GetAlgorithm(credentials),
                certificate.NotBefore,
                certificate.NotAfter,
                GetMetadata());

            IDictionary<string, string> GetMetadata()
            {
                var rsaParameters = CryptographyHelpers.GetRSAParameters(credentials);
                return new Dictionary<string, string>
                {
                    [JsonWebKeyParameterNames.E] = Base64UrlEncoder.Encode(rsaParameters.Exponent),
                    [JsonWebKeyParameterNames.N] = Base64UrlEncoder.Encode(rsaParameters.Modulus),
                };
            }
        }
    }
}
