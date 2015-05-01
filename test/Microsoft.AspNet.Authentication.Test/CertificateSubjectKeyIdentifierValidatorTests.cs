// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class CertificateSubjectKeyIdentifierValidatorTests
    {
        private static readonly X509Certificate2 SelfSigned = new X509Certificate2("selfSigned.cer");
        private static readonly X509Certificate2 Chained = new X509Certificate2("katanatest.redmond.corp.microsoft.com.cer");

        // The Katana test cert has a valid full chain
        // katanatest.redmond.corp.microsoft.com -> MSIT Machine Auth CA2 -> Microsoft Internet Authority -> Baltimore CyberTrustRoot

        private const string KatanaTestKeyIdentifier = "d964b2941aaf3e62761041b1f3db098edfa3270a";
        private const string MicrosoftInternetAuthorityKeyIdentifier = "2a4d97955d347e9db6e633be9c27c1707e67dbc1";

        [Fact]
        public void ConstructorShouldNotThrowWithValidValues()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(new[] { string.Empty });

            instance.ShouldNotBe(null);
        }

        [Fact]
        public void ConstructorShouldThrownWhenTheValidHashEnumerableIsNull()
        {
            Should.Throw<ArgumentNullException>(() =>
                new CertificateSubjectKeyIdentifierValidator(null));
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateChainErrors()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(new[] { string.Empty });
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateChainErrors);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateNameMismatch()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(new[] { string.Empty });
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateNameMismatch);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateNotAvailable()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(new[] { string.Empty });
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenPassedASelfSignedCertificate()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(new[] { string.Empty });
            var certificateChain = new X509Chain();
            certificateChain.Build(SelfSigned);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, SelfSigned, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenPassedATrustedCertificateWhichDoesNotHaveAWhitelistedSubjectKeyIdentifier()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(new[] { string.Empty });
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasItsSubjectKeyIdentifierWhiteListed()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(
                new[]
                {
                    KatanaTestKeyIdentifier
                });

            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasAChainElementSubjectKeyIdentifierWhiteListed()
        {
            var instance = new CertificateSubjectKeyIdentifierValidator(
                new[]
                {
                    MicrosoftInternetAuthorityKeyIdentifier
                });
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }
    }
}
