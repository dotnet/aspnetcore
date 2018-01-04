// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Service.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultSigningCredentialsSourceTest
    {
        [Fact]
        public async Task GetCredentialsAsync_ReadsCredentialsFromOptions()
        {
            // Arrange
            var reference = new DateTimeOffset(2000,01,01,00,00,00,TimeSpan.Zero);

            var identityServiceOptions = new IdentityServiceOptions();
            identityServiceOptions.SigningKeys.Add(new SigningCredentials(CryptoUtilities.CreateTestKey("RSAKey"), "RS256"));
            identityServiceOptions.SigningKeys.Add(new SigningCredentials(new X509SecurityKey(GetCertificate(reference)), "RS256"));
            var mock = new Mock<IOptionsSnapshot<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(identityServiceOptions);
            mock.Setup(m => m.Get(It.IsAny<string>())).Returns(identityServiceOptions);
            var source = new DefaultSigningCredentialsSource(mock.Object, new TestTimeStampManager(reference));

            // Act
            var credentials = (await source.GetCredentials()).ToList();

            // Assert
            Assert.Equal(2, credentials.Count);
            var rsaDescriptor = Assert.Single(credentials, c => c.Id == "RSAKey");
            Assert.Equal("RSA", rsaDescriptor.Algorithm);
            Assert.Equal(reference, rsaDescriptor.NotBefore);
            Assert.Equal(reference.AddDays(1), rsaDescriptor.Expires);
            Assert.Equal(identityServiceOptions.SigningKeys[0], rsaDescriptor.Credentials);
            Assert.True(rsaDescriptor.Metadata.ContainsKey("n"));
            Assert.True(rsaDescriptor.Metadata.ContainsKey("e"));

            var certificateDescriptor = Assert.Single(credentials, c => c.Id != "RSAKey");
            Assert.Equal("RSA", certificateDescriptor.Algorithm);
            Assert.Equal(reference, certificateDescriptor.NotBefore);
            Assert.Equal(reference.AddHours(1), certificateDescriptor.Expires);
            Assert.Equal(identityServiceOptions.SigningKeys[1], certificateDescriptor.Credentials);
            Assert.True(certificateDescriptor.Metadata.ContainsKey("n"));
            Assert.True(certificateDescriptor.Metadata.ContainsKey("e"));
        }

        private X509Certificate2 GetCertificate(DateTimeOffset reference)
        {
            // We are base64urlencoding a certificate that we generated on the fly due to
            // the fact that .NET Framework doesn't have the ability to generate self-signed
            // certificates on the flight.
            // The snippet below shows how to create a self-signed certificate in .NET Core 2.0
            // and Base65UrlEncode it so that it can be put in a string in code.
            // If for any reason this test needs to change, just regenerate a base 64 representation
            // of the certificate with the snippet below in .NET Core 2.0+ and se it on the rawCertificate.
            string rawCertificate = "MIICqTCCAZGgAwIBAgIJAIl7w4Jsl-LCMA0GCSqGSIb3DQEBCwUAMBQxEjAQBgNVBAMTCWxvY2FsaG9zdDAeFw0wMDAxMDEwMDAwMDBaFw0wMDAxMDEwMTAwMDBaMBQxEjAQBgNVBAMTCWxvY2FsaG9zdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAIgVCyY9FxKU5N0VSctfnyewoqsRg9S2Bf-bJOT-bl5BwmXhuOdOP2AVP0DbqhvuylsQ9gbJOK5qnzHjm0BtISFDLyA-V-cjodhYRsssPvTx0b04whFLHLH6uBkeHWUqDuP0ziQ1Ujb0nrmlUJ5XqYYBi1kfflH0imwxVxCCagTt-N3FyBfaU1dxR5MqN2U3Pj4Mmt0-sDlNoNDJPptqakHSnGPpP4KjM0h5jWmmjpRT_bKQYkQ2llDkQI7h1VUeT0tfGrGi8JowL0jrgfHJOFQ8r-xz_0IcAK4BdkmMAVR4SXDlK9lZX89pQfu20LK118B3NHtHJAm41NLYXr0LDv8CAwEAATANBgkqhkiG9w0BAQsFAAOCAQEAM09oxd6ehkPwqb8L9KKFKIFFdAggPICUkfeb68Ac_-DpAiUgtBE4vOGaIcO85ED5vreqNPImiCEczhJaAsTdn7fcloFKBafWsiuk9n1GnLTP69TOHg6-a4hdIJJhIJmA3qXOFcwvMU75hSm9tYzMmoQDPr09bVvfkH5WBaobr8iNpB9R8gSoe8NjIH57Q1RvRHWJ35lIuyNOtXbZKH-AvsAoeZaiVlSWI3xu5hq5deuDeQ-P8FnhyRNPRvp83fgou1N1WMZNK9T_RUSrxhqitUr8B7wUe2lRo0mcu1WHQ9_G6_5uV2LgpAHMM1p_JFyhE00o4JMnkeH3oFeYq_ml7Q";
            return new X509Certificate2(Base64UrlEncoder.DecodeBytes(rawCertificate));

            // var distinguishedName = new X500DistinguishedName("CN=localhost");
            // var rsaKey = RSA.Create(2048);
            // var request = new CertificateRequest(
            //     distinguishedName, rsaKey, HashAlgorithmName.SHA256);
            // var certificate = request.CreateSelfSigned(reference, reference + TimeSpan.FromHours(1));
            // var encoded = Base64UrlEncoder.Encode(certificate.Export(X509ContentType.Cert));
            //return certificate;
        }

        private class TestTimeStampManager : TimeStampManager
        {
            private readonly DateTimeOffset _reference;

            public TestTimeStampManager(DateTimeOffset reference)
            {
                _reference = reference;
            }
            public override DateTimeOffset GetCurrentTimeStampUtc() => _reference;
        }
    }
}
