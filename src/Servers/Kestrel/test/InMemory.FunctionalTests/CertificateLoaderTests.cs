// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class CertificateLoaderTests : LoggedTest
    {
        //private const string ServerAuthenticationEnhancedKeyUsageOid = "1.3.6.1.5.5.7.3.1";
        //private const string ServerAuthenticationEnhancedKeyUsageOidFriendlyName = "Server Authentication";

        [Theory]
        [InlineData("no_extensions.pfx")]
        public void IsCertificateAllowedForServerAuth_AllowWithNoExtensions(string testCertName)
        {
            var certPath = TestResources.GetCertPath(testCertName);
            TestOutputHelper.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.Empty(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());

            Assert.True(CertificateLoader.IsCertificateAllowedForServerAuth(cert));
        }

        [Theory]
        [InlineData("eku.server.pfx")]
        [InlineData("eku.multiple_usages.pfx")]
        public void IsCertificateAllowedForServerAuth_ValidatesEnhancedKeyUsageOnCertificate(string testCertName)
        {
            var certPath = TestResources.GetCertPath(testCertName);
            TestOutputHelper.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.NotEmpty(cert.Extensions);
            var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
            Assert.NotEmpty(eku.EnhancedKeyUsages);

            Assert.True(CertificateLoader.IsCertificateAllowedForServerAuth(cert));
        }

        [Theory]
        [InlineData("eku.code_signing.pfx")]
        [InlineData("eku.client.pfx")]
        public void IsCertificateAllowedForServerAuth_RejectsCertificatesMissingServerEku(string testCertName)
        {
            var certPath = TestResources.GetCertPath(testCertName);
            TestOutputHelper.WriteLine("Loading " + certPath);
            var cert = new X509Certificate2(certPath, "testPassword");
            Assert.NotEmpty(cert.Extensions);
            var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
            Assert.NotEmpty(eku.EnhancedKeyUsages);

            Assert.False(CertificateLoader.IsCertificateAllowedForServerAuth(cert));
        }

        //[Fact]
        //public void LoadFromStoreCert_MatchExact()
        //{
        //    var initialCertCount = ListCertificates(StoreName.My, StoreLocation.CurrentUser).Count;
        //    IList<X509Certificate2> certificates = null;

        //    try
        //    {
        //        var generatedCertificates = new HashSet<X509Certificate2>() {
        //            GenerateCertificate("a_temp_name"),
        //            GenerateCertificate("b_temp_name"),
        //            GenerateCertificate("temp_name"),
        //            GenerateCertificate("temp_name_c"),
        //            GenerateCertificate("temp_name_d"),
        //            GenerateCertificate("e_temp_name_f")
        //        };

        //        foreach (var cert in generatedCertificates)
        //        {
        //            CertificateManager.Instance.SaveCertificate(cert);
        //        }

        //        certificates = ListCertificates(StoreName.My, StoreLocation.CurrentUser);

        //        Assert.Equal(initialCertCount + generatedCertificates.Count, certificates.Count);

        //        var foundCertificate = CertificateLoader.LoadFromStoreCert("temp_name", StoreName.My.ToString(), StoreLocation.CurrentUser, true);
        //        Assert.Equal("temp_name", foundCertificate.GetNameInfo(X509NameType.SimpleName, true));
        //    }
        //    finally
        //    {
        //        if (certificates != null)
        //        {
        //            foreach (var cert in certificates)
        //            {
        //                if (cert.Subject.Contains("temp_name"))
        //                {
        //                    CertificateManager.Instance.RemoveCertificate(cert, CertificateManager.RemoveLocations.Local);
        //                }
        //            }
        //        }
        //    }
        //}

        //[Fact]
        //public void LoadFromStoreCert_FallbackToSubstring()
        //{
        //    var initialCertCount = ListCertificates(StoreName.My, StoreLocation.CurrentUser).Count;
        //    IList<X509Certificate2> certificates = null;

        //    try
        //    {
        //        var generatedCertificates = new HashSet<X509Certificate2>() {
        //            GenerateCertificate("temp_substr_name"),
        //        };

        //        foreach (var cert in generatedCertificates)
        //        {
        //            CertificateManager.Instance.SaveCertificate(cert);
        //        }

        //        certificates = ListCertificates(StoreName.My, StoreLocation.CurrentUser);

        //        Assert.Equal(initialCertCount + generatedCertificates.Count, certificates.Count);

        //        var foundCertificate = CertificateLoader.LoadFromStoreCert("substr", StoreName.My.ToString(), StoreLocation.CurrentUser, true);
        //        Assert.Equal("temp_substr_name", foundCertificate.GetNameInfo(X509NameType.SimpleName, true));
        //    }
        //    finally
        //    {
        //        if (certificates != null)
        //        {
        //            foreach (var cert in certificates)
        //            {
        //                if (cert.Subject.Contains("temp"))
        //                {
        //                    CertificateManager.Instance.RemoveCertificate(cert, CertificateManager.RemoveLocations.Local);
        //                }
        //            }
        //        }
        //    }
        //}

        //[Fact]
        //public void LoadFromStoreCert_NotFound()
        //{
        //    Assert.Throws<InvalidOperationException>(() => CertificateLoader.LoadFromStoreCert("NonExistent", StoreName.My.ToString(), StoreLocation.CurrentUser, true));
        //}

        //private IList<X509Certificate2> ListCertificates(StoreName storeName, StoreLocation storeLocation)
        //{
        //    var certificates = new List<X509Certificate2>();
        //    using (var store = new X509Store(storeName, storeLocation))
        //    {
        //        store.Open(OpenFlags.ReadOnly);
        //        certificates.AddRange(store.Certificates.OfType<X509Certificate2>());
        //        store.Close();
        //    }
        //    return certificates;
        //}

        //private X509Certificate2 GenerateCertificate(string name)
        //{
        //    var extensions = new List<X509Extension>();

        //    var basicConstraints = new X509BasicConstraintsExtension(
        //        certificateAuthority: false,
        //        hasPathLengthConstraint: false,
        //        pathLengthConstraint: 0,
        //        critical: true);

        //    var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, critical: true);

        //    var enhancedKeyUsage = new X509EnhancedKeyUsageExtension(
        //        new OidCollection() {
        //            new Oid(
        //                ServerAuthenticationEnhancedKeyUsageOid,
        //                ServerAuthenticationEnhancedKeyUsageOidFriendlyName)
        //        },
        //        critical: true);

        //    extensions.Add(basicConstraints);
        //    extensions.Add(keyUsage);
        //    extensions.Add(enhancedKeyUsage);

        //    var cert = CertificateManager.Instance.CreateSelfSignedCertificate(new X500DistinguishedName($"CN={name}"),
        //        extensions,
        //        DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));

        //    return cert;
        //}
    }
}
