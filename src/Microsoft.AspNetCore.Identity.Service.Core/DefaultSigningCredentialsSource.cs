// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service.Core
{
    public class DefaultSigningCredentialsSource : ISigningCredentialsSource
    {
        private readonly IOptions<IdentityServiceOptions> _options;
        private readonly ITimeStampManager _timeStampManager;

        public DefaultSigningCredentialsSource(
            IOptions<IdentityServiceOptions> options,
            ITimeStampManager timeStampManager)
        {
            _options = options;
            _timeStampManager = timeStampManager;
        }

        public Task<IEnumerable<SigningCredentialsDescriptor>> GetCredentials()
        {
            var descriptors = GetDescriptors(_options.Value);
            return Task.FromResult(descriptors);
        }

        private IEnumerable<SigningCredentialsDescriptor> GetDescriptors(IdentityServiceOptions options)
        {
            return options.SigningKeys.Select(sk =>
            {
                var validity = GetValidity(sk);
                return new SigningCredentialsDescriptor(
                    sk,
                    CryptographyHelpers.GetAlgorithm(sk),
                    validity.NotBefore,
                    validity.Expires,
                    GetMetadata(sk));
            });
        }

        private Validity GetValidity(SigningCredentials credentials)
        {
            var x509SecurityKey = credentials.Key as X509SecurityKey;
            if (x509SecurityKey != null)
            {
                var certificate = x509SecurityKey.Certificate;
                return new Validity
                {
                    NotBefore = certificate.NotBefore.ToUniversalTime(),
                    Expires = certificate.NotAfter.ToUniversalTime()
                };
            }

            var rsaSecurityKey = credentials.Key as RsaSecurityKey;
            if (rsaSecurityKey != null)
            {
                return new Validity
                {
                    NotBefore = _timeStampManager.GetCurrentTimeStampUtcAsDateTime(),
                    Expires = _timeStampManager.GetTimeStampUtcAsDateTime(TimeSpan.FromDays(1))
                };
            }

            throw new NotSupportedException();
        }

        private struct Validity
        {
            public DateTimeOffset NotBefore;
            public DateTimeOffset Expires;
        }

        private IDictionary<string, string> GetMetadata(SigningCredentials credentials)
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
