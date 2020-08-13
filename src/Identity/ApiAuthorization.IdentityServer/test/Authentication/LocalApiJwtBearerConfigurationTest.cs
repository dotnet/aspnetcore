// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    public class IdentityServerJwtBearerOptionsConfigurationTest
    {
        [Fact]
        public void Configure_SetsUpBearerSchemeForTheLocalApi()
        {
            // Arrange
            var localApiDescriptor = new Mock<IIdentityServerJwtDescriptor>();
            localApiDescriptor.Setup(lad => lad.GetResourceDefinitions())
                .Returns(new Dictionary<string, ResourceDefinition>
                {
                    ["TestAPI"] = new ResourceDefinition { Profile = ApplicationProfiles.IdentityServerJwt }
                });

            var bearerConfiguration = new IdentityServerJwtBearerOptionsConfiguration(
                "authScheme",
                "TestAPI",
                localApiDescriptor.Object);

            var options = new JwtBearerOptions();

            // Act
            bearerConfiguration.Configure("authScheme", options);

            // Assert
            Assert.Equal("name", options.TokenValidationParameters.NameClaimType);
            Assert.Equal("role", options.TokenValidationParameters.RoleClaimType);
            Assert.Equal("TestAPI", options.Audience);
        }

        [Fact]
        public async Task ResolveAuthorityAndKeysAsync_SetsUpAuthorityAndKeysOnTheTokenValidationParametersAsync()
        {
            // Arrange
            var localApiDescriptor = new Mock<IIdentityServerJwtDescriptor>();
            localApiDescriptor.Setup(lad => lad.GetResourceDefinitions())
                .Returns(new Dictionary<string, ResourceDefinition>
                {
                    ["TestAPI"] = new ResourceDefinition { Profile = ApplicationProfiles.IdentityServerJwt }
                });

            var credentialsStore = new Mock<ISigningCredentialStore>();
            var key = new RsaSecurityKey(RSA.Create());
            credentialsStore.Setup(cs => cs.GetSigningCredentialsAsync())
                            .ReturnsAsync(new SigningCredentials(key, "RS256"));

            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("localhost");
            context.RequestServices = new ServiceCollection()
                            .AddSingleton(new IdentityServerOptions())
                            .AddSingleton(credentialsStore.Object)
                            .BuildServiceProvider();

            var options = new JwtBearerOptions();
            var args = new MessageReceivedContext(context, new AuthenticationScheme("TestAPI",null, Mock.Of<IAuthenticationHandler>().GetType()), options);
            
            // Act
            await IdentityServerJwtBearerOptionsConfiguration.ResolveAuthorityAndKeysAsync(args);

            // Assert
            Assert.Equal("https://localhost", options.TokenValidationParameters.ValidIssuer);
            Assert.Equal(key, options.TokenValidationParameters.IssuerSigningKey);
        }

        [Fact]
        public void Configure_IgnoresOptionsForDifferentSchemes()
        {
            // Arrange
            var localApiDescriptor = new Mock<IIdentityServerJwtDescriptor>();
            localApiDescriptor.Setup(lad => lad.GetResourceDefinitions())
                .Returns(new Dictionary<string, ResourceDefinition>
                {
                    ["TestAPI"] = new ResourceDefinition { Profile = ApplicationProfiles.IdentityServerJwt }
                });

            var bearerConfiguration = new IdentityServerJwtBearerOptionsConfiguration(
                "authScheme",
                "TestAPI",
                localApiDescriptor.Object);

            var options = new JwtBearerOptions();

            // Act
            bearerConfiguration.Configure("otherScheme", options);

            // Assert
            Assert.NotEqual("name", options.TokenValidationParameters.NameClaimType);
            Assert.NotEqual("role", options.TokenValidationParameters.RoleClaimType);
            Assert.NotEqual("TestAPI", options.Audience);
            Assert.NotEqual("https://localhost", options.Authority);
        }

        [Fact]
        public void Configure_IgnoresOptionsForNonExistingAPIs()
        {
            // Arrange
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("localhost");
            context.RequestServices = new ServiceCollection()
                .AddSingleton(new IdentityServerOptions())
                .BuildServiceProvider();
            contextAccessor.SetupGet(ca => ca.HttpContext).Returns(
                context);

            var localApiDescriptor = new Mock<IIdentityServerJwtDescriptor>();
            localApiDescriptor.Setup(lad => lad.GetResourceDefinitions())
                .Returns(new Dictionary<string, ResourceDefinition>
                {
                    ["TestAPI"] = new ResourceDefinition { Profile = ApplicationProfiles.IdentityServerJwt }
                });

            var credentialsStore = new Mock<ISigningCredentialStore>();
            var key = new RsaSecurityKey(RSA.Create());
            credentialsStore.Setup(cs => cs.GetSigningCredentialsAsync())
                .ReturnsAsync(new SigningCredentials(key, "RS256"));

            var bearerConfiguration = new IdentityServerJwtBearerOptionsConfiguration(
                "authScheme",
                "NonExistingApi",
                localApiDescriptor.Object);

            var options = new JwtBearerOptions();

            // Act
            bearerConfiguration.Configure("authScheme", options);

            // Assert
            Assert.NotEqual("name", options.TokenValidationParameters.NameClaimType);
            Assert.NotEqual("role", options.TokenValidationParameters.RoleClaimType);
            Assert.NotEqual(key, options.TokenValidationParameters.IssuerSigningKey);
            Assert.NotEqual("TestAPI", options.Audience);
            Assert.NotEqual("https://localhost", options.Authority);
        }
    }
}
