// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class AzureADB2CAuthenticationBuilderExtensionsTests
    {
        [Fact]
        public void AddAzureADB2C_AddsAllAuthenticationHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureADB2C(o => { });
            var provider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(provider.GetService<OpenIdConnectHandler>());
            Assert.NotNull(provider.GetService<CookieAuthenticationHandler>());
            Assert.NotNull(provider.GetService<PolicySchemeHandler>());
        }

        [Fact]
        public void AddAzureADB2C_ConfiguresAllOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureADB2C(o =>
                {
                    o.Instance = "https://login.microsoftonline.com/tfp";
                    o.ClientId = "ClientId";
                    o.ClientSecret = "ClientSecret";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.SignUpSignInPolicyId = "B2C_1_SiUpIn";
                    o.ResetPasswordPolicyId = "B2C_1_SSPR";
                    o.EditProfilePolicyId = "B2C_1_SiPe";
                });
            var provider = services.BuildServiceProvider();

            // Assert
            var azureADB2COptionsMonitor = provider.GetService<IOptionsMonitor<AzureADB2COptions>>();
            Assert.NotNull(azureADB2COptionsMonitor);
            var azureADB2COptions = azureADB2COptionsMonitor.Get(AzureADB2CDefaults.AuthenticationScheme);
            Assert.Equal(AzureADB2CDefaults.OpenIdScheme, azureADB2COptions.OpenIdConnectSchemeName);
            Assert.Equal(AzureADB2CDefaults.CookieScheme, azureADB2COptions.CookieSchemeName);
            Assert.Equal("https://login.microsoftonline.com/tfp", azureADB2COptions.Instance);
            Assert.Equal("ClientId", azureADB2COptions.ClientId);
            Assert.Equal("ClientSecret", azureADB2COptions.ClientSecret);
            Assert.Equal("/signin-oidc", azureADB2COptions.CallbackPath);
            Assert.Equal("domain.onmicrosoft.com", azureADB2COptions.Domain);
            Assert.Equal("B2C_1_SiUpIn", azureADB2COptions.SignUpSignInPolicyId);
            Assert.Equal("B2C_1_SSPR", azureADB2COptions.ResetPasswordPolicyId);
            Assert.Equal("B2C_1_SiPe", azureADB2COptions.EditProfilePolicyId);

            var openIdOptionsMonitor = provider.GetService<IOptionsMonitor<OpenIdConnectOptions>>();
            Assert.NotNull(openIdOptionsMonitor);
            var openIdOptions = openIdOptionsMonitor.Get(AzureADB2CDefaults.OpenIdScheme);
            Assert.Equal("ClientId", openIdOptions.ClientId);
            Assert.Equal($"https://login.microsoftonline.com/tfp/domain.onmicrosoft.com/B2C_1_SiUpIn/v2.0", openIdOptions.Authority);
            Assert.True(openIdOptions.UseTokenLifetime);
            Assert.Equal("/signin-oidc", openIdOptions.CallbackPath);
            Assert.Equal(AzureADB2CDefaults.CookieScheme, openIdOptions.SignInScheme);
            Assert.NotNull(openIdOptions.TokenValidationParameters);
            Assert.Equal("name", openIdOptions.TokenValidationParameters.NameClaimType);
            Assert.NotNull(openIdOptions.Events);
            var redirectHandler = openIdOptions.Events.OnRedirectToIdentityProvider;
            Assert.NotNull(redirectHandler);
            Assert.IsType<AzureADB2COpenIDConnectEventHandlers>(redirectHandler.Target);
            var remoteFailureHanlder = openIdOptions.Events.OnRemoteFailure;
            Assert.NotNull(remoteFailureHanlder);
            Assert.IsType<AzureADB2COpenIDConnectEventHandlers>(redirectHandler.Target);
        }

        [Fact]
        public void AddAzureADB2C_ThrowsForDuplicatedSchemes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADB2C(o => { })
                .AddAzureADB2C(o => { });

            var provider = services.BuildServiceProvider();
            var azureADB2COptionsMonitor = provider.GetService<IOptionsMonitor<AzureADB2COptions>>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADB2COptionsMonitor.Get(AzureADB2CDefaults.AuthenticationScheme));

            Assert.Equal("A scheme with the name 'AzureADB2C' was already added.", exception.Message);
        }

        [Fact]
        public void AddAzureADB2C_ThrowsWhenOpenIdSchemeIsAlreadyInUse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADB2C(o => { })
                .AddAzureADB2C("Custom", AzureADB2CDefaults.OpenIdScheme, "Cookie", null, o => { });

            var provider = services.BuildServiceProvider();
            var azureADB2COptionsMonitor = provider.GetService<IOptionsMonitor<AzureADB2COptions>>();

            var expectedMessage = $"The Open ID Connect scheme 'AzureADB2COpenID' can't be associated with the Azure Active Directory B2C scheme 'Custom'. " +
                "The Open ID Connect scheme 'AzureADB2COpenID' is already mapped to the Azure Active Directory B2C scheme 'AzureADB2C'";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADB2COptionsMonitor.Get(AzureADB2CDefaults.AuthenticationScheme));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void AddAzureADB2C_ThrowsWhenCookieSchemeIsAlreadyInUse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADB2C(o => { })
                .AddAzureADB2C("Custom", "OpenID", AzureADB2CDefaults.CookieScheme, null, o => { });

            var provider = services.BuildServiceProvider();
            var azureADB2COptionsMonitor = provider.GetService<IOptionsMonitor<AzureADB2COptions>>();

            var expectedMessage = $"The cookie scheme 'AzureADB2CCookie' can't be associated with the Azure Active Directory B2C scheme 'Custom'. " +
                "The cookie scheme 'AzureADB2CCookie' is already mapped to the Azure Active Directory B2C scheme 'AzureADB2C'";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADB2COptionsMonitor.Get(AzureADB2CDefaults.AuthenticationScheme));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void AddAzureADB2CBearer_AddsAllAuthenticationHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureADB2CBearer(o => { });
            var provider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(provider.GetService<JwtBearerHandler>());
            Assert.NotNull(provider.GetService<PolicySchemeHandler>());
        }

        [Fact]
        public void AddAzureADB2CBearer_ConfiguresAllOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureADB2CBearer(o =>
                {
                    o.Instance = "https://login.microsoftonline.com/tfp";
                    o.ClientId = "ClientId";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.SignUpSignInPolicyId = "B2C_1_SiUpIn";
                });
            var provider = services.BuildServiceProvider();

            // Assert
            var azureADB2COptionsMonitor = provider.GetService<IOptionsMonitor<AzureADB2COptions>>();
            Assert.NotNull(azureADB2COptionsMonitor);
            var options = azureADB2COptionsMonitor.Get(AzureADB2CDefaults.BearerAuthenticationScheme);
            Assert.Equal(AzureADB2CDefaults.JwtBearerAuthenticationScheme, options.JwtBearerSchemeName);
            Assert.Equal("https://login.microsoftonline.com/tfp", options.Instance);
            Assert.Equal("ClientId", options.ClientId);
            Assert.Equal("domain.onmicrosoft.com", options.Domain);
            Assert.Equal("B2C_1_SiUpIn", options.DefaultPolicy);

            var bearerOptionsMonitor = provider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            Assert.NotNull(bearerOptionsMonitor);
            var bearerOptions = bearerOptionsMonitor.Get(AzureADB2CDefaults.JwtBearerAuthenticationScheme);
            Assert.Equal("ClientId", bearerOptions.Audience);
            Assert.Equal($"https://login.microsoftonline.com/tfp/domain.onmicrosoft.com/B2C_1_SiUpIn/v2.0", bearerOptions.Authority);
        }

        [Fact]
        public void AddAzureADB2CBearer_ThrowsForDuplicatedSchemes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADB2CBearer(o => { })
                .AddAzureADB2CBearer(o => { });

            var provider = services.BuildServiceProvider();
            var azureADB2COptionsMonitor = provider.GetService<IOptionsMonitor<AzureADB2COptions>>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADB2COptionsMonitor.Get(AzureADB2CDefaults.AuthenticationScheme));

            Assert.Equal("A scheme with the name 'AzureADB2CBearer' was already added.", exception.Message);
        }

        [Fact]
        public void AddAzureADB2CBearer_ThrowsWhenBearerSchemeIsAlreadyInUse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADB2CBearer(o => { })
                .AddAzureADB2CBearer("Custom", AzureADB2CDefaults.JwtBearerAuthenticationScheme, o => { });

            var provider = services.BuildServiceProvider();
            var azureADB2COptionsMonitor = provider.GetService<IOptionsMonitor<AzureADB2COptions>>();

            var expectedMessage = $"The JSON Web Token Bearer scheme 'AzureADB2CJwtBearer' can't be associated with the Azure Active Directory B2C scheme 'Custom'. " +
                "The JSON Web Token Bearer scheme 'AzureADB2CJwtBearer' is already mapped to the Azure Active Directory B2C scheme 'AzureADB2CBearer'";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADB2COptionsMonitor.Get(AzureADB2CDefaults.AuthenticationScheme));

            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
