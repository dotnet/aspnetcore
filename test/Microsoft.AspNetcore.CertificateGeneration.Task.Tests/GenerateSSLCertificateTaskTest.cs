// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microsoft.AspNetCore.CertificateGeneration.Task
{
    public class GenerateSSLCertificateTaskTest : IDisposable
    {
        private const string TestSubject = "CN=test.ssl.localhost";

        [Fact]
        public void GenerateSSLCertificateTaskTest_CreatesCertificate_IfNoCertificateIsFound()
        {
            // Arrange
            EnsureCleanUp();
            var task = new TestGenerateSSLCertificateTask();

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            var certificates = GetTestCertificates();
            Assert.Single(certificates);
            Assert.Single(task.Messages);
            Assert.StartsWith($"Generated certificate {TestSubject}", task.Messages[0]);
        }

        [Fact]
        public void GenerateSSLCertificateTaskTest_CreatesCertificate_IfFoundCertificateHasExpired()
        {
            // Arrange
            EnsureCleanUp();
            CreateCertificate(notBefore: DateTimeOffset.UtcNow.AddYears(-2), expires: DateTimeOffset.UtcNow.AddYears(-1));

            var task = new TestGenerateSSLCertificateTask();

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            var certificates = GetTestCertificates();
            Assert.Equal(2, certificates.Count);
            Assert.Single(task.Messages);
            Assert.StartsWith($"Generated certificate {TestSubject}", task.Messages[0]);
        }

        [Fact]
        public void GenerateSSLCertificateTaskTest_CreatesCertificate_IfFoundCertificateIsNotYetValid()
        {
            // Arrange
            EnsureCleanUp();
            CreateCertificate(notBefore: DateTimeOffset.UtcNow.AddYears(1), expires: DateTimeOffset.UtcNow.AddYears(2));

            var task = new TestGenerateSSLCertificateTask();

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            var certificates = GetTestCertificates();
            Assert.Equal(2, certificates.Count);
            Assert.Equal(1, task.Messages.Count);
            Assert.StartsWith($"Generated certificate {TestSubject}", task.Messages[0]);
        }

        [Fact]
        public void GenerateSSLCertificateTaskTest_CreatesCertificate_IfFoundCertificateDoesNotHavePrivateKeys()
        {
            // Arrange
            EnsureCleanUp();
            CreateCertificate(savePrivateKey: false);
            var task = new TestGenerateSSLCertificateTask();

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            var certificates = GetTestCertificates();
            Assert.Equal(2, certificates.Count);
            Assert.Single(task.Messages);
            Assert.StartsWith($"Generated certificate {TestSubject}", task.Messages[0]);
        }

        [Fact]
        public void GenerateSSLCertificateTaskTest_DoesNothing_IfValidCertificateIsFound()
        {
            // Arrange
            EnsureCleanUp();
            CreateCertificate();
            var task = new TestGenerateSSLCertificateTask();

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            var certificates = GetTestCertificates();
            Assert.Single(certificates);
            Assert.Single(task.Messages);
            Assert.Equal($"A certificate with subject name '{TestSubject}' already exists. Skipping certificate generation.", task.Messages[0]);
        }

        [Fact]
        public void GenerateSSLCertificateTaskTest_CreatesACertificateWhenThereIsAlreadyAValidCertificate_IfForceIsSpecified()
        {
            // Arrange
            EnsureCleanUp();
            CreateCertificate();
            var task = new TestGenerateSSLCertificateTask() { Force = true };

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            var certificates = GetTestCertificates();
            Assert.Equal(2, certificates.Count);
            Assert.Single(task.Messages);
            Assert.StartsWith($"Generated certificate {TestSubject}", task.Messages[0]);
        }

        public X509CertificateCollection GetTestCertificates()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, TestSubject, validOnly: false);
                store.Close();

                return certificates;
            }
        }

        private void EnsureCleanUp()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, TestSubject, validOnly: false);
                store.RemoveRange(certificates);
                store.Close();
            }
        }

        public void Dispose()
        {
            EnsureCleanUp();
        }

        private void CreateCertificate(
            DateTimeOffset notBefore = default(DateTimeOffset),
            DateTimeOffset expires = default(DateTimeOffset),
            bool savePrivateKey = true)
        {
            using (var rsa = RSA.Create(2048))
            {
                var signingRequest = new CertificateRequest(
                    new X500DistinguishedName(TestSubject), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var enhancedKeyUsage = new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")
                };
                signingRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, critical: true));
                signingRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, critical: true));
                signingRequest.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(
                        certificateAuthority: false,
                        hasPathLengthConstraint: false,
                        pathLengthConstraint: 0,
                        critical: true));

                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(TestSubject.Replace("CN=", ""));
                signingRequest.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = signingRequest.CreateSelfSigned(
                    notBefore == default(DateTimeOffset) ? DateTimeOffset.Now : notBefore,
                    expires == default(DateTimeOffset) ? DateTimeOffset.Now.AddYears(1) : expires);


                var imported = certificate;
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && savePrivateKey)
                {
                    var export = certificate.Export(X509ContentType.Pkcs12, "");

                    imported = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet);
                    Array.Clear(export, 0, export.Length);
                }
                else if (!savePrivateKey)
                {
                    var export = certificate.Export(X509ContentType.Cert, "");

                    imported = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet);
                    Array.Clear(export, 0, export.Length);
                }

                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(imported);
                    store.Close();
                };
            }
        }

        private class TestGenerateSSLCertificateTask : GenerateSSLCertificateTask
        {
            public TestGenerateSSLCertificateTask()
            {
                Subject = TestSubject;
            }

            public IList<string> Messages { get; set; } = new List<string>();

            protected override void LogMessage(string message) => Messages.Add(message);
        }
    }
}
