// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class CertificateThumbprintValidatorTests
    {
        private static readonly X509Certificate2 SelfSigned = new X509Certificate2("selfSigned.cer");
        private static readonly X509Certificate2 Chained = new X509Certificate2("katanatest.redmond.corp.microsoft.com.cer");

        // The Katana test cert has a valid full chain
        // katanatest.redmond.corp.microsoft.com -> MSIT Machine Auth CA2 -> Microsoft Internet Authority -> Baltimore CyberTrustRoot

        private const string KatanaTestThumbprint = "a9894c464b260cac3f5b91cece33b3c55e82e61c";
        private const string MicrosoftInternetAuthorityThumbprint = "992ad44d7dce298de17e6f2f56a7b9caa41db93f";

        [Fact]
        public void ConstructorShouldNotThrowWithValidValues()
        {
            var instance = new CertificateThumbprintValidator(new string[1]);

            instance.ShouldNotBe(null);
        }

        [Fact]
        public void ConstructorShouldThrownWhenTheValidHashEnumerableIsNull()
        {
            Should.Throw<ArgumentNullException>(() =>
                new CertificateThumbprintValidator(null));
        }

        [Fact]
        public void ConstructorShouldThrowWhenTheHashEnumerableContainsNoHashes()
        {
            Should.Throw<ArgumentOutOfRangeException>(() =>
                new CertificateThumbprintValidator(new string[0]));
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateChainErrors()
        {
            var instance = new CertificateThumbprintValidator(new string[1]);
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateChainErrors);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateNameMismatch()
        {
            var instance = new CertificateThumbprintValidator(new string[1]);
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateNameMismatch);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateNotAvailable()
        {
            var instance = new CertificateThumbprintValidator(new string[1]);
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenPassedASelfSignedCertificate()
        {
            var instance = new CertificateThumbprintValidator(new string[1]);
            var certificateChain = new X509Chain();
            certificateChain.Build(SelfSigned);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, SelfSigned, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenPassedATrustedCertificateWhichDoesNotHaveAWhitelistedThumbprint()
        {
            var instance = new CertificateThumbprintValidator(new string[1]);
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasItsThumbprintWhiteListed()
        {
            var instance = new CertificateThumbprintValidator(new[] { KatanaTestThumbprint });
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasAChainElementThumbprintWhiteListed()
        {
            var instance = new CertificateThumbprintValidator(new[] { MicrosoftInternetAuthorityThumbprint });
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }
    }
}
