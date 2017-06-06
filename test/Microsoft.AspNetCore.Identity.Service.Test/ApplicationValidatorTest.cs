// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service.Test
{
    public class ApplicationValidatorTest
    {
        public ApplicationErrorDescriber errorDescriber = new ApplicationErrorDescriber();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("~/")]
        [InlineData("0123456789012345678901234567789001234567890")]
        public async Task ValidateApplication_FailsForInvalidNames(string name)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication(name: name, clientId: Guid.NewGuid().ToString());
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError>
            {
                errorDescriber.InvalidApplicationName(name)
            };

            // Act
            var result = await validator.ValidateAsync(manager, application);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Theory]
        [InlineData("TestApplication")]
        [InlineData("testapplication")]
        [InlineData("TESTAPPLICATION")]
        public async Task ValidateApplication_FailsForDuplicateApplicationNames(string name)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication("ApplicationId", name, Guid.NewGuid().ToString());
            var manager = CreateTestManager(duplicateName: true);

            var expectedError = new List<IdentityServiceError>
            {
                errorDescriber.DuplicateApplicationName(name)
            };

            // Act
            var result = await validator.ValidateAsync(manager, application);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("~/")]
        [InlineData("0123456789012345678901234567789001234567890")]
        public async Task ValidateApplication_FailsForInvalidClientIds(string clientId)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication(clientId: clientId);
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError>
            {
                errorDescriber.InvalidApplicationClientId(clientId)
            };

            // Act
            var result = await validator.ValidateAsync(manager, application);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Theory]
        [InlineData("ClientId")]
        [InlineData("clientid")]
        [InlineData("CLIENTID")]
        public async Task ValidateApplication_FailsForDuplicateClientIds(string clientId)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication("ApplicationId", "TestApplication", clientId);
            var manager = CreateTestManager(duplicateClientId: true);

            var expectedError = new List<IdentityServiceError>
            {
                errorDescriber.DuplicateApplicationClientId(clientId)
            };

            // Act
            var result = await validator.ValidateAsync(manager, application);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateApplication_SucceedsWhenNameAndClientIdAreValid()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            // Act
            var result = await validator.ValidateAsync(manager, application);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Theory]
        [InlineData("urn:ietf:wg:oauth:2.0:oob")]
        [InlineData("URN:IETF:WG:OAUTH:2.0:OOB")]
        [InlineData("https://www.example.com/signout-oidc")]
        [InlineData("HTTPS://WWW.EXAMPLE.COM/SIGNOUT-OIDC")]
        public async Task ValidateLogoutUri_FailsIfTheApplicationAlreadyContainsTheUri(string logoutUri)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.DuplicateLogoutUri(logoutUri) };

            // Act
            var result = await validator.ValidateLogoutUriAsync(manager, application, logoutUri);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact(Skip="https://github.com/aspnet/Identity/issues/1266")]
        public async Task ValidateLogoutUri_FailsIfTheUriIsRelative()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.InvalidLogoutUri("/signout-oidc") };

            // Act
            var result = await validator.ValidateLogoutUriAsync(manager, application, "/signout-oidc");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateLogoutUri_FailsIfTheUriIsNotHttps()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.NoHttpsUri("http://www.example.com/signout-oidc") };

            // Act
            var result = await validator.ValidateLogoutUriAsync(manager, application, "http://www.example.com/signout-oidc");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateLogoutUri_FailsIfTheUriIsNotInTheSameDomainAsTheOthers()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.DifferentDomains() };

            // Act
            var result = await validator.ValidateLogoutUriAsync(manager, application, "https://www.contoso.com/signout-oidc");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateLogoutUri_FailsFailsForOtherNonHttpsUris()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> {
                errorDescriber.NoHttpsUri("urn:self:aspnet:identity:integrated"),
                errorDescriber.DifferentDomains()
            };

            // Act
            var result = await validator.ValidateLogoutUriAsync(manager, application, "urn:self:aspnet:identity:integrated");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Theory]
        [InlineData("https://www.example.com/another-path")]
        [InlineData("HTTPS://WWW.EXAMPLE.COM/ANOTHER-PATH")]
        public async Task ValidateLogoutUri_SucceedsForOtherUrisOnTheSameDomain(string logoutUri)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            // Act
            var result = await validator.ValidateLogoutUriAsync(manager, application, logoutUri);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Theory]
        [InlineData("urn:ietf:wg:oauth:2.0:oob")]
        [InlineData("URN:IETF:WG:OAUTH:2.0:OOB")]
        [InlineData("https://www.example.com/signin-oidc")]
        [InlineData("HTTPS://WWW.EXAMPLE.COM/SIGNIN-OIDC")]
        public async Task ValidateRedirectUri_FailsIfTheApplicationAlreadyContainsTheUri(string redirectUri)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.DuplicateRedirectUri(redirectUri) };

            // Act
            var result = await validator.ValidateRedirectUriAsync(manager, application, redirectUri);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact(Skip="https://github.com/aspnet/Identity/issues/1266")]
        public async Task ValidateRedirectUri_FailsIfTheUriIsRelative()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.InvalidRedirectUri("/signin-oidc") };

            // Act
            var result = await validator.ValidateRedirectUriAsync(manager, application, "/signin-oidc");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateRedirectUri_FailsIfTheUriIsNotHttps()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.NoHttpsUri("http://www.example.com/signin-oidc") };

            // Act
            var result = await validator.ValidateRedirectUriAsync(manager, application, "http://www.example.com/signin-oidc");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateRedirectUri_FailsIfTheUriIsNotInTheSameDomainAsTheOthers()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> { errorDescriber.DifferentDomains() };

            // Act
            var result = await validator.ValidateRedirectUriAsync(manager, application, "https://www.contoso.com/signin-oidc");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateRedirectUri_FailsForOtherNonHttpsUris()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError> {
                errorDescriber.NoHttpsUri("urn:self:aspnet:identity:integrated"),
                errorDescriber.DifferentDomains()
            };

            // Act
            var result = await validator.ValidateRedirectUriAsync(manager, application, "urn:self:aspnet:identity:integrated");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Theory]
        [InlineData("https://www.example.com/another-path")]
        [InlineData("HTTPS://WWW.EXAMPLE.COM/ANOTHER-PATH")]
        public async Task ValidateRedirectUri_SucceedsForOtherUrisOnTheSameDomain(string redirectUri)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            // Act
            var result = await validator.ValidateRedirectUriAsync(manager, application, redirectUri);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("~/")]
        [InlineData("012345678901234567890")]
        public async Task ValidateScope_FailsForInvalidScopeValues(string scope)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError>
            {
                errorDescriber.InvalidScope(scope)
            };

            // Act
            var result = await validator.ValidateScopeAsync(manager, application, scope);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Theory]
        [InlineData("openid")]
        [InlineData("openID")]
        [InlineData("OPENID")]
        public async Task ValidateScope_FailsForDuplicateScopeValues(string scope)
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            var expectedError = new List<IdentityServiceError>
            {
                errorDescriber.DuplicateScope(scope)
            };

            // Act
            var result = await validator.ValidateScopeAsync(manager, application, scope);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
        }

        [Fact]
        public async Task ValidateScope_SucceedsWhenScopesAreValid()
        {
            // Arrange
            var validator = new ApplicationValidator<TestApplication>(new ApplicationErrorDescriber());
            var application = CreateApplication();
            var manager = CreateTestManager();

            // Act
            var result = await validator.ValidateScopeAsync(manager, application, "offline_access");

            // Assert
            Assert.True(result.Succeeded);
        }

        private TestApplication CreateApplication(
            string id = "Id",
            string name = "TestApplication",
            string clientId = "ClientId") => new TestApplication
            {
                Id = id,
                Name = name,
                ClientId = clientId,
                RedirectUris = new List<string>
                {
                    "urn:ietf:wg:oauth:2.0:oob",
                    "https://www.example.com/signin-oidc"
                },
                LogoutUris = new List<string>
                {
                    "urn:ietf:wg:oauth:2.0:oob",
                    "https://www.example.com/signout-oidc"
                },
                Scopes = new List<string> { "openid" }
            };

        private ApplicationManager<TestApplication> CreateTestManager(bool duplicateName = false, bool duplicateClientId = false)
        {
            var otherApplication = CreateApplication();

            var store = new Mock<IApplicationStore<TestApplication>>();
            if (duplicateName)
            {
                store.Setup(s => s.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(otherApplication);
            }

            if (duplicateClientId)
            {
            store.Setup(s => s.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherApplication);
            }

            store.Setup(s => s.GetApplicationIdAsync(It.IsAny<TestApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync<TestApplication, CancellationToken, IApplicationStore<TestApplication>, string>((a, ct) => a.Id);

            store.Setup(s => s.GetApplicationNameAsync(It.IsAny<TestApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync<TestApplication, CancellationToken, IApplicationStore<TestApplication>, string>((a, ct) => a.Name);

            store.Setup(s => s.GetApplicationClientIdAsync(It.IsAny<TestApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync<TestApplication, CancellationToken, IApplicationStore<TestApplication>, string>((a, ct) => a.ClientId);

            store.As<IRedirectUriStore<TestApplication>>()
                .Setup(s => s.FindRegisteredUrisAsync(It.IsAny<TestApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherApplication.RedirectUris);

            store.As<IRedirectUriStore<TestApplication>>()
                .Setup(s => s.FindRegisteredLogoutUrisAsync(It.IsAny<TestApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherApplication.LogoutUris);

            store.As<IApplicationScopeStore<TestApplication>>()
                .Setup(s => s.FindScopesAsync(It.IsAny<TestApplication>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherApplication.Scopes);

            return new ApplicationManager<TestApplication>(
                Options.Create(new ApplicationOptions()),
                store.Object,
                Mock.Of<IPasswordHasher<TestApplication>>(),
                Enumerable.Empty<IApplicationValidator<TestApplication>>(),
                Mock.Of<ILogger<ApplicationManager<TestApplication>>>(),
                new ApplicationErrorDescriber());
        }

        private class ErrorsComparer : IEqualityComparer<IEnumerable<IdentityServiceError>>
        {
            public static ErrorsComparer Instance = new ErrorsComparer();

            public bool Equals(
                IEnumerable<IdentityServiceError> left,
                IEnumerable<IdentityServiceError> right)
            {
                var leftOrdered = left.OrderBy(o => o.Code).ThenBy(o => o.Description).ToArray();
                var rightOrdered = right.OrderBy(o => o.Code).ThenBy(o => o.Description).ToArray();

                return leftOrdered.Length == rightOrdered.Length &&
                    leftOrdered.Select((s, i) => s.Code.Equals(rightOrdered[i].Code) &&
                    s.Description.Equals(rightOrdered[i].Description)).All(a => a);
            }

            public int GetHashCode(IEnumerable<IdentityServiceError> obj)
            {
                return 1;
            }
        }

        public class TestApplication
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string ClientId { get; set; }
            public List<string> RedirectUris { get; set; }
            public List<string> LogoutUris { get; set; }
            public List<string> Scopes { get; set; }
        }
    }
}
