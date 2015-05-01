// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class CertificateSubjectPublicKeyInfoValidatorTests
    {
        private static readonly X509Certificate2 SelfSigned = new X509Certificate2("selfSigned.cer");
        private static readonly X509Certificate2 Chained = new X509Certificate2("katanatest.redmond.corp.microsoft.com.cer");

        // The Katana test cert has a valid full chain
        // katanatest.redmond.corp.microsoft.com -> MSIT Machine Auth CA2 -> Microsoft Internet Authority -> Baltimore CyberTrustRoot

        // The following fingerprints were generated using the go program in appendix A of the Public Key Pinning Extension for HTTP
        // draft-ietf-websec-key-pinning-05

        private const string KatanaTestSha1Hash = "xvNsCWwxvL3qsCYChZLiwNm1D6o=";
        private const string KatanaTestSha256Hash = "AhR1Y/xhxK2uD7YJ0xKUPq8tYrWm4+F7DgO2wUOqB+4=";

        private const string MicrosoftInternetAuthoritySha1Hash = "Z3HnseSVDEPu5hZoj05/bBSnT/s=";
        private const string MicrosoftInternetAuthoritySha256Hash = "UQTPeq/Tlg/vLt2ijtl7qlMFBFkbGG9aAWJbQMOMWFg=";

        [Fact]
        public void ConstructorShouldNotThrowWithValidValues()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new string[1], SubjectPublicKeyInfoAlgorithm.Sha1);

            instance.ShouldNotBe(null);
        }

        [Fact]
        public void ConstructorShouldThrownWhenTheValidHashEnumerableIsNull()
        {
            Should.Throw<ArgumentNullException>(() =>
                new CertificateSubjectPublicKeyInfoValidator(null, SubjectPublicKeyInfoAlgorithm.Sha1));
        }

        [Fact]
        public void ConstructorShouldThrowWhenTheHashEnumerableContainsNoHashes()
        {
            Should.Throw<ArgumentOutOfRangeException>(() =>
                new CertificateSubjectPublicKeyInfoValidator(new string[0], SubjectPublicKeyInfoAlgorithm.Sha1));
        }

        [Fact]
        public void ConstructorShouldThrowIfAnInvalidAlgorithmIsPassed()
        {
            Should.Throw<ArgumentOutOfRangeException>(() =>
                new CertificateSubjectPublicKeyInfoValidator(new string[0], (SubjectPublicKeyInfoAlgorithm)2));
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateChainErrors()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new string[1], SubjectPublicKeyInfoAlgorithm.Sha1);
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateChainErrors);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateNameMismatch()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new string[1], SubjectPublicKeyInfoAlgorithm.Sha1);
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateNameMismatch);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenSslPolicyErrorsIsRemoteCertificateNotAvailable()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new string[1], SubjectPublicKeyInfoAlgorithm.Sha1);
            bool result = instance.Validate(null, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);
            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenPassedASelfSignedCertificate()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new string[1], SubjectPublicKeyInfoAlgorithm.Sha1);
            var certificateChain = new X509Chain();
            certificateChain.Build(SelfSigned);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, SelfSigned, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenPassedATrustedCertificateWhichDoesNotHaveAWhitelistedSha1Spki()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new string[1], SubjectPublicKeyInfoAlgorithm.Sha1);
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasItsSha1SpkiWhiteListed()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new[] { KatanaTestSha1Hash }, SubjectPublicKeyInfoAlgorithm.Sha1);
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasAChainElementSha1SpkiWhiteListed()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new[] { MicrosoftInternetAuthoritySha1Hash }, SubjectPublicKeyInfoAlgorithm.Sha1);
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }

        [Fact]
        public void ValidatorShouldReturnFalseWhenPassedATrustedCertificateWhichDoesNotHaveAWhitelistedSha256Spki()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new string[1], SubjectPublicKeyInfoAlgorithm.Sha256);
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(false);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasItsSha256SpkiWhiteListed()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new[] { KatanaTestSha256Hash }, SubjectPublicKeyInfoAlgorithm.Sha256);
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }

        [Fact]
        public void ValidatorShouldReturnTrueWhenPassedATrustedCertificateWhichHasAChainElementSha256SpkiWhiteListed()
        {
            var instance = new CertificateSubjectPublicKeyInfoValidator(new[] { MicrosoftInternetAuthoritySha256Hash }, SubjectPublicKeyInfoAlgorithm.Sha256);
            var certificateChain = new X509Chain();
            certificateChain.Build(Chained);
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            bool result = instance.Validate(null, Chained, certificateChain, SslPolicyErrors.None);

            result.ShouldBe(true);
        }
    }
}
