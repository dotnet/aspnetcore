// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultSigningCredentialsPolicyProviderTest
    {
        [Fact]
        public async Task GetAllCredentialsAsync_GetsCredentialsFromAllSources()
        {
            // Arrange
            var descriptors = new List<SigningCredentialsDescriptor>()
            {
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials(),
                    "RSA",
                    DateTimeOffset.Now+TimeSpan.FromHours(1),
                    DateTimeOffset.Now+TimeSpan.FromHours(2),
                    new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials(),
                    "RSA",
                    DateTimeOffset.Now,
                    DateTimeOffset.Now+TimeSpan.FromHours(1),
                    new Dictionary<string,string>()),
            };

            var expected = descriptors.ToList();
            expected.Reverse();

            var mockSource = new Mock<ISigningCredentialsSource>();
            mockSource.Setup(scs => scs.GetCredentials())
                .ReturnsAsync(descriptors);

            var sources = new List<ISigningCredentialsSource>()
            {
                mockSource.Object
            };

            var policyProvider = new DefaultSigningCredentialsPolicyProvider(sources, new TimeStampManager(), new HostingEnvironment());

            // Act
            var credentials = await policyProvider.GetAllCredentialsAsync();

            // Assert
            Assert.Equal(expected, credentials);
        }

        [Fact]
        public async Task GetAllCredentialsAsync_RetrievesTheCredentialsIfAllOfThemAreExpired()
        {
            // Arrange
            var descriptors1 = new List<SigningCredentialsDescriptor>()
            {
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("First"),
                    "RSA",
                    DateTimeOffset.Now-TimeSpan.FromHours(2),
                    DateTimeOffset.Now-TimeSpan.FromHours(1),
                    new Dictionary<string,string>())
            };

            var descriptors2 = new List<SigningCredentialsDescriptor>()
            {
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("First"),
                    "RSA",
                    DateTimeOffset.Now-TimeSpan.FromHours(2),
                    DateTimeOffset.Now-TimeSpan.FromHours(1),
                    new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Second"),
                    "RSA",
                    DateTimeOffset.Now,
                    DateTimeOffset.Now+TimeSpan.FromHours(1),
                    new Dictionary<string,string>())
            };

            var expected = descriptors2.ToList();

            var mockSource = new Mock<ISigningCredentialsSource>();
            mockSource.SetupSequence(s => s.GetCredentials())
                .ReturnsAsync(descriptors1)
                .ReturnsAsync(descriptors2);

            var sources = new List<ISigningCredentialsSource>()
            {
                mockSource.Object
            };

            var policyProvider = new DefaultSigningCredentialsPolicyProvider(sources, new TimeStampManager(), new HostingEnvironment());

            // Act
            var credentials = await policyProvider.GetAllCredentialsAsync();
            credentials = await policyProvider.GetAllCredentialsAsync();

            // Assert
            Assert.Equal(expected, credentials);
        }

        [Fact]
        public async Task GetAllCredentialsAsync_RetrievesCredentialsInOrder()
        {
            // Arrange
            var reference = DateTimeOffset.UtcNow;

            var descriptors = new List<SigningCredentialsDescriptor>()
            {
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Fourth"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(3),
                    notBefore: reference+TimeSpan.FromHours(1),
                    metadata: new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Third"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(2),
                    notBefore: reference+TimeSpan.FromHours(1),
                    metadata: new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Second"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(2),
                    notBefore: reference,
                    metadata: new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("First"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(1),
                    notBefore: reference,
                    metadata: new Dictionary<string,string>())
            };

            var mockSource = new Mock<ISigningCredentialsSource>();
            mockSource.Setup(s => s.GetCredentials())
                .ReturnsAsync(descriptors);

            var expected = descriptors.ToList();
            expected.Reverse();

            var sources = new List<ISigningCredentialsSource>()
            {
                mockSource.Object
            };

            var policyProvider = new DefaultSigningCredentialsPolicyProvider(sources, new TimeStampManager(), new HostingEnvironment());

            // Act
            var credentials = await policyProvider.GetAllCredentialsAsync();

            // Assert
            Assert.Equal(expected, credentials);
        }

        [Fact]
        public async Task GetSigningCredentialsAsync_RetrievesTheCredentialWithEarliestExpirationAndAllowedUsage()
        {
            // Arrange
            var reference = DateTimeOffset.UtcNow;
            var expected = new SigningCredentialsDescriptor(
                    CreateRsaCredentials("First"),
                    "RSA",
                    expires: reference + TimeSpan.FromHours(1),
                    notBefore: reference,
                    metadata: new Dictionary<string, string>());

            var descriptors = new List<SigningCredentialsDescriptor>()
            {
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Fourth"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(3),
                    notBefore: reference+TimeSpan.FromHours(1),
                    metadata: new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Third"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(2),
                    notBefore: reference+TimeSpan.FromHours(1),
                    metadata: new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Second"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(2),
                    notBefore: reference,
                    metadata: new Dictionary<string,string>()),
                    expected
            };

            var mockSource = new Mock<ISigningCredentialsSource>();
            mockSource.Setup(s => s.GetCredentials())
                .ReturnsAsync(descriptors);

            var sources = new List<ISigningCredentialsSource>()
            {
                mockSource.Object
            };

            var policyProvider = new DefaultSigningCredentialsPolicyProvider(sources, new TimeStampManager(), new HostingEnvironment());

            // Act
            var signingCredential = await policyProvider.GetSigningCredentialsAsync();

            // Assert
            Assert.Equal(expected, signingCredential);
        }

        [Fact]
        public async Task GetSigningCredentialsAsync_SkipsExpiredCredentials()
        {
            // Arrange
            var reference = DateTimeOffset.UtcNow;
            var expired = new SigningCredentialsDescriptor(
                CreateRsaCredentials("First"),
                "RSA",
                expires: reference - TimeSpan.FromHours(1),
                notBefore: reference - TimeSpan.FromHours(2),
                metadata: new Dictionary<string, string>());

            var expected = new SigningCredentialsDescriptor(
                CreateRsaCredentials("Second"),
                "RSA",
                expires: reference + TimeSpan.FromHours(2),
                notBefore: reference,
                metadata: new Dictionary<string, string>());


            var descriptors = new List<SigningCredentialsDescriptor>()
            {
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Fourth"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(3),
                    notBefore: reference+TimeSpan.FromHours(1),
                    metadata: new Dictionary<string,string>()),
                new SigningCredentialsDescriptor(
                    CreateRsaCredentials("Third"),
                    "RSA",
                    expires: reference+TimeSpan.FromHours(2),
                    notBefore: reference+TimeSpan.FromHours(1),
                    metadata: new Dictionary<string,string>()),
                expected,
                expired
            };

            var mockSource = new Mock<ISigningCredentialsSource>();
            mockSource.Setup(s => s.GetCredentials())
                .ReturnsAsync(descriptors);

            var sources = new List<ISigningCredentialsSource>()
            {
                mockSource.Object
            };

            var policyProvider = new DefaultSigningCredentialsPolicyProvider(sources, new TimeStampManager(), new HostingEnvironment());

            // Act
            var signingCredential = await policyProvider.GetSigningCredentialsAsync();

            // Assert
            Assert.Equal(expected, signingCredential);
        }

        private SigningCredentials CreateRsaCredentials(string id = "Test") =>
            new SigningCredentials(CryptoUtilities.CreateTestKey(id), "RSA");
    }
}
