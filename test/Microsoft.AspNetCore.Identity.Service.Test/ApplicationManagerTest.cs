// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ApplicationManagerTest
    {
        public static readonly ApplicationErrorDescriber ErrorDescriber = new ApplicationErrorDescriber();

        [Fact]
        public async Task CreateCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.CreateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.CreateAsync(application);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task CreateCallsValidators()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Success);

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.CreateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.CreateAsync(application);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
            validator.VerifyAll();
        }

        [Fact]
        public async Task CreateValidatorsCanBlockCreate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var store = new Mock<IApplicationStore<TestApplication>>();

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.CreateAsync(application);

            // Assert
            Assert.False(result.Succeeded);
            store.VerifyAll();
            validator.VerifyAll();
        }

        [Fact]
        public async Task UpdateCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.UpdateAsync(application);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateCallsValidators()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Success);

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.UpdateAsync(application);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
            validator.VerifyAll();
        }

        [Fact]
        public async Task UpdateValidatorsCanBlockCreate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var store = new Mock<IApplicationStore<TestApplication>>();

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.UpdateAsync(application);

            // Assert
            Assert.False(result.Succeeded);
            store.VerifyAll();
            validator.VerifyAll();
        }

        [Fact]
        public async Task DeleteCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.DeleteAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.DeleteAsync(application);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindByIdCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.FindByIdAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(application)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var foundApplication = await applicationManager.FindByIdAsync("id");

            // Assert
            Assert.Equal(application, foundApplication);
            store.VerifyAll();
        }

        [Fact]
        public async Task GetApplicationIdCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.GetApplicationIdAsync(application, CancellationToken.None))
                .ReturnsAsync("id")
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var id = await applicationManager.GetApplicationIdAsync(application);

            // Assert
            Assert.Equal("id", id);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindByClientIdCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.FindByClientIdAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(application)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var foundApplication = await applicationManager.FindByClientIdAsync("ClientId");

            // Assert
            Assert.Equal(application, foundApplication);
            store.VerifyAll();
        }

        [Fact]
        public async Task GetApplicationClientIdCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.GetApplicationClientIdAsync(application, CancellationToken.None))
                .ReturnsAsync("clientId")
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var clientId = await applicationManager.GetApplicationClientIdAsync(application);

            // Assert
            Assert.Equal("clientId", clientId);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindByNameCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.FindByNameAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(application)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var foundApplication = await applicationManager.FindByNameAsync("Application");

            // Assert
            Assert.Equal(application, foundApplication);
            store.VerifyAll();
        }

        [Fact]
        public async Task GetApplicationNameCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.GetApplicationNameAsync(application, CancellationToken.None))
                .ReturnsAsync("Application")
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var name = await applicationManager.GetApplicationNameAsync(application);

            // Assert
            Assert.Equal("Application", name);
            store.VerifyAll();
        }

        [Fact]
        public async Task SetApplicationNameCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.SetApplicationNameAsync(application, "Updated", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            store.Setup(s => s.UpdateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.SetApplicationNameAsync(application, "Updated");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task SetApplicationNameCallsValidators()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.SetApplicationNameAsync(application, "Updated", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            store.Setup(s => s.UpdateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.SetApplicationNameAsync(application, "Updated");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task SetApplicationNameValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.SetApplicationNameAsync(application, "Updated", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.SetApplicationNameAsync(application, "Updated");

            // Assert
            Assert.False(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task HasClientSecretCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();
            store.Setup(s => s.HasClientSecretAsync(application, CancellationToken.None))
                .ReturnsAsync(true)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var hasClientSecret = await applicationManager.HasClientSecretAsync(application);

            // Assert
            Assert.True(hasClientSecret);
            store.VerifyAll();
        }

        [Fact]
        public async Task AddClientSecretCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();

            store.Setup(s => s.GetClientSecretHashAsync(application, CancellationToken.None))
                .ReturnsAsync((string)null)
                .Verifiable();

            store.Setup(s => s.SetClientSecretHashAsync(application, "new-hash", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            store.Setup(s => s.UpdateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.AddClientSecretAsync(application, "client-secret");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task AddClientSecretFailsIfTheresAlreadyASecret()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };
            var expectedError = new List<IdentityServiceError> { ErrorDescriber.ApplicationAlreadyHasClientSecret() };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();

            store.Setup(s => s.GetClientSecretHashAsync(application, CancellationToken.None))
                .ReturnsAsync("hash")
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.AddClientSecretAsync(application, "client-secret");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);

            store.VerifyAll();
        }

        [Fact]
        public async Task AddClientSecretFailsIfValidatorBlocksTheUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();

            store.Setup(s => s.GetClientSecretHashAsync(application, CancellationToken.None))
                .ReturnsAsync((string)null)
                .Verifiable();

            store.Setup(s => s.SetClientSecretHashAsync(application, "new-hash", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.AddClientSecretAsync(application, "client-secret");

            // Assert
            Assert.False(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task ChangeClientSecretCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();

            store.Setup(s => s.SetClientSecretHashAsync(application, "new-hash", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            store.Setup(s => s.UpdateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.ChangeClientSecretAsync(application, "client-secret");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task ChangeClientSecretFailsIfValidatorBlocksTheUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();

            store.Setup(s => s.SetClientSecretHashAsync(application, "new-hash", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.ChangeClientSecretAsync(application, "client-secret");

            // Assert
            Assert.False(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task RemoveClientSecretCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();

            store.Setup(s => s.SetClientSecretHashAsync(application, null, CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            store.Setup(s => s.UpdateAsync(application, CancellationToken.None))
                .ReturnsAsync(IdentityServiceResult.Success)
                .Verifiable();

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.RemoveClientSecretAsync(application);

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task RemoveClientSecretFailsIfValidatorBlocksTheUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>()
                .As<IApplicationClientSecretStore<TestApplication>>();

            store.Setup(s => s.SetClientSecretHashAsync(application, null, CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<ApplicationManager<TestApplication>>(), application))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(store.Object, application, validator.Object);

            // Act
            var result = await applicationManager.RemoveClientSecretAsync(application);

            // Assert
            Assert.False(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task ValidateClientCredentialsCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.FindByClientIdAsync("clientId", It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var secretStore = store.As<IApplicationClientSecretStore<TestApplication>>();
            secretStore.Setup(s => s.HasClientSecretAsync(application, CancellationToken.None))
                .ReturnsAsync(true)
                .Verifiable();

            secretStore.Setup(s => s.GetClientSecretHashAsync(application, CancellationToken.None))
                .ReturnsAsync("hash")
                .Verifiable();

            var applicationManager = GetApplicationManager(secretStore.Object, application);

            // Act
            var result = await applicationManager.ValidateClientCredentialsAsync("clientId", "client-secret");

            // Assert
            Assert.True(result);
            secretStore.VerifyAll();
        }

        [Fact]
        public async Task ValidateClientCredentialsUpdatesHashIfNeeded()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.FindByClientIdAsync("clientId", It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var secretStore = store.As<IApplicationClientSecretStore<TestApplication>>();
            secretStore.Setup(s => s.HasClientSecretAsync(application, CancellationToken.None))
                .ReturnsAsync(true)
                .Verifiable();

            secretStore.Setup(s => s.GetClientSecretHashAsync(application, CancellationToken.None))
                .ReturnsAsync("hash")
                .Verifiable();

            secretStore.Setup(s => s.SetClientSecretHashAsync(application, "new-hash", CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var applicationManager = GetApplicationManager(secretStore.Object, application, null, PasswordVerificationResult.SuccessRehashNeeded);

            // Act
            var result = await applicationManager.ValidateClientCredentialsAsync("clientId", "client-secret");

            // Assert
            Assert.True(result);
            secretStore.VerifyAll();
        }

        [Fact]
        public async Task ValidateClientCredentialsReturnsFalseIfClientCredentialsValidationFails()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.FindByClientIdAsync("clientId", It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var secretStore = store.As<IApplicationClientSecretStore<TestApplication>>();
            secretStore.Setup(s => s.HasClientSecretAsync(application, CancellationToken.None))
                .ReturnsAsync(true)
                .Verifiable();

            secretStore.Setup(s => s.GetClientSecretHashAsync(application, CancellationToken.None))
                .ReturnsAsync("hash")
                .Verifiable();

            var applicationManager = GetApplicationManager(secretStore.Object, application, null, PasswordVerificationResult.Failed);

            // Act
            var result = await applicationManager.ValidateClientCredentialsAsync("clientId", "client-secret");

            // Assert
            Assert.False(result);
            secretStore.VerifyAll();
        }

        [Fact]
        public async Task FindRegisteredUrisCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();

            store.As<IRedirectUriStore<TestApplication>>()
                .Setup(s => s.FindRegisteredUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.FindRegisteredUrisAsync(application);

            // Assert
            Assert.Equal(new[] { "https://www.example.com/sign-in" }, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task RegisterRedirectUriCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var redirectUriStore = store.As<IRedirectUriStore<TestApplication>>();
            redirectUriStore.Setup(s => s.RegisterRedirectUriAsync(application, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(redirectUriStore.Object, application);

            // Act
            var result = await applicationManager.RegisterRedirectUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.True(result.Succeeded);
            redirectUriStore.VerifyAll();
        }

        [Fact]
        public async Task RegisterRedirectUriValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var redirectUriStore = new Mock<IRedirectUriStore<TestApplication>>();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateRedirectUriAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<string>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(redirectUriStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.RegisterRedirectUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.False(result.Succeeded);
            redirectUriStore.VerifyAll();
        }

        [Fact]
        public async Task UpdateRedirectUriCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var redirectUriStore = store.As<IRedirectUriStore<TestApplication>>();
            redirectUriStore.Setup(s => s.FindRegisteredUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            redirectUriStore.Setup(s => s.UpdateRedirectUriAsync(application, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(redirectUriStore.Object, application);

            // Act
            var result = await applicationManager.UpdateRedirectUriAsync(application, "https://www.example.com/sign-in", "https://www.example.com/signin");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateRedirectUriFailsIfItDoesNotFindTheUriToUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };
            var expectedError = new[] { ErrorDescriber.RedirectUriNotFound("https://www.example.com/sign-in") };

            var store = new Mock<IApplicationStore<TestApplication>>();
            var redirectUriStore = store.As<IRedirectUriStore<TestApplication>>();
            redirectUriStore.Setup(s => s.FindRegisteredUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { });

            var applicationManager = GetApplicationManager(redirectUriStore.Object, application);

            // Act
            var result = await applicationManager.UpdateRedirectUriAsync(application, "https://www.example.com/sign-in", "https://www.example.com/signin");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateRedirectUriValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var redirectUriStore = new Mock<IRedirectUriStore<TestApplication>>();
            redirectUriStore.Setup(s => s.FindRegisteredUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateRedirectUriAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<string>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(redirectUriStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.UpdateRedirectUriAsync(application, "https://www.example.com/sign-in", "https://www.example.com/signin");

            // Assert
            Assert.False(result.Succeeded);
            redirectUriStore.VerifyAll();
        }

        [Fact]
        public async Task UnregisterRedirectUriCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var redirectUriStore = store.As<IRedirectUriStore<TestApplication>>();
            redirectUriStore.Setup(s => s.FindRegisteredUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            redirectUriStore.Setup(s => s.UnregisterRedirectUriAsync(application, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(redirectUriStore.Object, application);

            // Act
            var result = await applicationManager.UnregisterRedirectUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task UnregisterRedirectUriFailsIfItDoesNotFindTheUriToUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };
            var expectedError = new[] { ErrorDescriber.RedirectUriNotFound("https://www.example.com/sign-in") };

            var store = new Mock<IApplicationStore<TestApplication>>();
            var redirectUriStore = store.As<IRedirectUriStore<TestApplication>>();
            redirectUriStore.Setup(s => s.FindRegisteredUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { });

            var applicationManager = GetApplicationManager(redirectUriStore.Object, application);

            // Act
            var result = await applicationManager.UnregisterRedirectUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindRegisteredLogoutUrisCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();

            store.As<IRedirectUriStore<TestApplication>>()
                .Setup(s => s.FindRegisteredLogoutUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var result = await applicationManager.FindRegisteredLogoutUrisAsync(application);

            // Assert
            Assert.Equal(new[] { "https://www.example.com/sign-in" }, result);
            store.VerifyAll();
        }

        [Fact]
        public async Task RegisterLogoutUriCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var logoutUriStore = store.As<IRedirectUriStore<TestApplication>>();
            logoutUriStore.Setup(s => s.RegisterLogoutRedirectUriAsync(application, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(logoutUriStore.Object, application);

            // Act
            var result = await applicationManager.RegisterLogoutUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.True(result.Succeeded);
            logoutUriStore.VerifyAll();
        }

        [Fact]
        public async Task RegisterLogoutUriValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var logoutUriStore = new Mock<IRedirectUriStore<TestApplication>>();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateLogoutUriAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<string>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(logoutUriStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.RegisterLogoutUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.False(result.Succeeded);
            logoutUriStore.VerifyAll();
        }

        [Fact]
        public async Task UpdateLogoutUriCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var logoutUriStore = store.As<IRedirectUriStore<TestApplication>>();
            logoutUriStore.Setup(s => s.FindRegisteredLogoutUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            logoutUriStore.Setup(s => s.UpdateLogoutRedirectUriAsync(application, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(logoutUriStore.Object, application);

            // Act
            var result = await applicationManager.UpdateLogoutUriAsync(application, "https://www.example.com/sign-in", "https://www.example.com/signin");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateLogoutUriFailsIfItDoesNotFindTheUriToUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };
            var expectedError = new[] { ErrorDescriber.LogoutUriNotFound("https://www.example.com/sign-in") };

            var store = new Mock<IApplicationStore<TestApplication>>();
            var logoutUriStore = store.As<IRedirectUriStore<TestApplication>>();
            logoutUriStore.Setup(s => s.FindRegisteredLogoutUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { });

            var applicationManager = GetApplicationManager(logoutUriStore.Object, application);

            // Act
            var result = await applicationManager.UpdateLogoutUriAsync(application, "https://www.example.com/sign-in", "https://www.example.com/signin");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateLogoutUriValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var logoutUriStore = new Mock<IRedirectUriStore<TestApplication>>();
            logoutUriStore.Setup(s => s.FindRegisteredLogoutUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateLogoutUriAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<string>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(logoutUriStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.UpdateLogoutUriAsync(application, "https://www.example.com/sign-in", "https://www.example.com/signin");

            // Assert
            Assert.False(result.Succeeded);
            logoutUriStore.VerifyAll();
        }

        [Fact]
        public async Task UnregisterLogoutUriCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var logoutUriStore = store.As<IRedirectUriStore<TestApplication>>();
            logoutUriStore.Setup(s => s.FindRegisteredLogoutUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "https://www.example.com/sign-in" });

            logoutUriStore.Setup(s => s.UnregisterLogoutRedirectUriAsync(application, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(logoutUriStore.Object, application);

            // Act
            var result = await applicationManager.UnregisterLogoutUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task UnregisterLogoutUriFailsIfItDoesNotFindTheUriToUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };
            var expectedError = new[] { ErrorDescriber.LogoutUriNotFound("https://www.example.com/sign-in") };

            var store = new Mock<IApplicationStore<TestApplication>>();
            var logoutUriStore = store.As<IRedirectUriStore<TestApplication>>();
            logoutUriStore.Setup(s => s.FindRegisteredLogoutUrisAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { });

            var applicationManager = GetApplicationManager(logoutUriStore.Object, application);

            // Act
            var result = await applicationManager.UnregisterLogoutUriAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindScopesAsyncCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationScopeStore<TestApplication>>();
            store.Setup(s => s.FindScopesAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "openid" });

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var scopes = await applicationManager.FindScopesAsync(application);

            // Assert
            Assert.Equal(new[] { "openid" }, scopes);
        }

        [Fact]
        public async Task AddScopeCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var scopeStore = store.As<IApplicationScopeStore<TestApplication>>();
            scopeStore.Setup(s => s.AddScopeAsync(application, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(scopeStore.Object, application);

            // Act
            var result = await applicationManager.AddScopeAsync(application, "https://www.example.com/sign-in");

            // Assert
            Assert.True(result.Succeeded);
            scopeStore.VerifyAll();
        }

        [Fact]
        public async Task AddScopeValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var scopeStore = new Mock<IApplicationScopeStore<TestApplication>>();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateScopeAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<string>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(scopeStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.AddScopeAsync(application, "openid");

            // Assert
            Assert.False(result.Succeeded);
            scopeStore.VerifyAll();
        }

        [Fact]
        public async Task UpdateScopeCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var scopeStore = store.As<IApplicationScopeStore<TestApplication>>();
            scopeStore.Setup(s => s.FindScopesAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "openid" });

            scopeStore.Setup(s => s.UpdateScopeAsync(application, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(scopeStore.Object, application);

            // Act
            var result = await applicationManager.UpdateScopeAsync(application, "openid", "offline_access");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateScopeFailsIfItDoesNotFindTheUriToUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };
            var expectedError = new[] { ErrorDescriber.ScopeNotFound("openid") };

            var store = new Mock<IApplicationStore<TestApplication>>();
            var scopeStore = store.As<IApplicationScopeStore<TestApplication>>();
            scopeStore.Setup(s => s.FindScopesAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { });

            var applicationManager = GetApplicationManager(scopeStore.Object, application);

            // Act
            var result = await applicationManager.UpdateScopeAsync(application, "openid", "offline_access");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
            store.VerifyAll();
        }

        [Fact]
        public async Task UpdateScopeValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var scopeStore = new Mock<IApplicationScopeStore<TestApplication>>();
            scopeStore.Setup(s => s.FindScopesAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "openid" });

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateScopeAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<string>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(scopeStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.UpdateScopeAsync(application, "openid", "offline_access");

            // Assert
            Assert.False(result.Succeeded);
            scopeStore.VerifyAll();
        }

        [Fact]
        public async Task RemoveScopeCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var scopeStore = store.As<IApplicationScopeStore<TestApplication>>();
            scopeStore.Setup(s => s.FindScopesAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "openid" });

            scopeStore.Setup(s => s.RemoveScopeAsync(application, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var applicationManager = GetApplicationManager(scopeStore.Object, application);

            // Act
            var result = await applicationManager.RemoveScopeAsync(application, "openid");

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task RemoveScopeFailsIfItDoesNotFindTheUriToUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };
            var expectedError = new[] { ErrorDescriber.ScopeNotFound("openid") };

            var store = new Mock<IApplicationStore<TestApplication>>();
            var scopeStore = store.As<IApplicationScopeStore<TestApplication>>();
            scopeStore.Setup(s => s.FindScopesAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new string[] { });

            var applicationManager = GetApplicationManager(scopeStore.Object, application);

            // Act
            var result = await applicationManager.RemoveScopeAsync(application, "openid");

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(expectedError, result.Errors, ErrorsComparer.Instance);
            store.VerifyAll();
        }

        [Fact]
        public async Task FindClaimsAsyncCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationClaimStore<TestApplication>>();
            var expectedClaims = new[] { new Claim("type", "value") };
            store.Setup(s => s.GetClaimsAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedClaims);

            var applicationManager = GetApplicationManager(store.Object, application);

            // Act
            var claims = await applicationManager.GetClaimsAsync(application);

            // Assert
            Assert.Equal(expectedClaims, claims);
        }

        [Fact]
        public async Task AddClaimCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var claimsStore = store.As<IApplicationClaimStore<TestApplication>>();
            claimsStore.Setup(s => s.AddClaimsAsync(application, It.IsAny<IEnumerable<Claim>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var applicationManager = GetApplicationManager(claimsStore.Object, application);

            // Act
            var result = await applicationManager.AddClaimAsync(application, new Claim("type", "value"));

            // Assert
            Assert.True(result.Succeeded);
            claimsStore.VerifyAll();
        }

        [Fact]
        public async Task AddClaimValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var claimsStore = new Mock<IApplicationClaimStore<TestApplication>>();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateClaimAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(claimsStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.AddClaimAsync(application, new Claim("type","value"));

            // Assert
            Assert.False(result.Succeeded);
            claimsStore.VerifyAll();
        }

        [Fact]
        public async Task ReplaceClaimCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var claimsStore = store.As<IApplicationClaimStore<TestApplication>>();
            claimsStore.Setup(s => s.ReplaceClaimAsync(application, It.IsAny<Claim>(), It.IsAny<Claim>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var applicationManager = GetApplicationManager(claimsStore.Object, application);

            // Act
            var result = await applicationManager.ReplaceClaimAsync(application, new Claim("type", "value"), new Claim("new-type", "new-value"));

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        [Fact]
        public async Task ReplaceClaimValidatorsCanBlockUpdate()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var claimsStore = new Mock<IApplicationClaimStore<TestApplication>>();

            var validator = new Mock<IApplicationValidator<TestApplication>>();
            validator.Setup(s => s.ValidateClaimAsync(It.IsAny<ApplicationManager<TestApplication>>(), application, It.IsAny<Claim>()))
                .ReturnsAsync(IdentityServiceResult.Failed(new IdentityServiceError()));

            var applicationManager = GetApplicationManager(claimsStore.Object, application, validator.Object);

            // Act
            var result = await applicationManager.ReplaceClaimAsync(application, new Claim("type", "value"), new Claim("new-type", "new-value"));

            // Assert
            Assert.False(result.Succeeded);
            claimsStore.VerifyAll();
        }

        [Fact]
        public async Task RemoveClaimCallsStore()
        {
            // Arrange
            var application = new TestApplication { Name = "Application", ClientId = "ClientId" };

            var store = new Mock<IApplicationStore<TestApplication>>();
            store.Setup(s => s.UpdateAsync(application, It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityServiceResult.Success);

            var claimsStore = store.As<IApplicationClaimStore<TestApplication>>();

            claimsStore.Setup(s => s.RemoveClaimsAsync(application, It.IsAny<IEnumerable<Claim>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var applicationManager = GetApplicationManager(claimsStore.Object, application);

            // Act
            var result = await applicationManager.RemoveClaimAsync(application, new Claim("type", "value"));

            // Assert
            Assert.True(result.Succeeded);
            store.VerifyAll();
        }

        private ApplicationManager<TestApplication> GetApplicationManager(
            IApplicationStore<TestApplication> store,
            TestApplication application,
            IApplicationValidator<TestApplication> validator = null,
            PasswordVerificationResult hashResult = PasswordVerificationResult.Success)
        {
            var hasher = new Mock<IPasswordHasher<TestApplication>>();
            hasher.Setup(s => s.HashPassword(application, "client-secret"))
                .Returns("new-hash");

            hasher.Setup(s => s.VerifyHashedPassword(application, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(hashResult);

            return new ApplicationManager<TestApplication>(
                Options.Create(new ApplicationOptions()),
                store,
                hasher.Object,
                validator == null ? Enumerable.Empty<IApplicationValidator<TestApplication>>() : new List<IApplicationValidator<TestApplication>> { validator },
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
            public string Name { get; internal set; }
            public string ClientId { get; internal set; }
            public List<string> RedirectUris { get; internal set; }
            public List<string> LogoutUris { get; internal set; }
            public List<string> Scopes { get; internal set; }
        }
    }
}
