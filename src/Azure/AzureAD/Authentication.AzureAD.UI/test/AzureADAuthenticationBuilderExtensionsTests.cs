// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class AzureADAuthenticationBuilderExtensionsTests
    {
        [Fact]
        public void AddAzureAD_AddsAllAuthenticationHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureAD(o => { });
            var provider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(provider.GetService<OpenIdConnectHandler>());
            Assert.NotNull(provider.GetService<CookieAuthenticationHandler>());
            Assert.NotNull(provider.GetService<PolicySchemeHandler>());
        }

        [Fact]
        public void AddAzureAD_ConfiguresAllOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureAD(o =>
                {
                    o.Instance = "https://login.microsoftonline.com";
                    o.ClientId = "ClientId";
                    o.ClientSecret = "ClientSecret";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.TenantId = "Common";
                });
            var provider = services.BuildServiceProvider();

            // Assert
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();
            Assert.NotNull(azureADOptionsMonitor);
            var azureADOptions = azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme);
            Assert.Equal(AzureADDefaults.OpenIdScheme, azureADOptions.OpenIdConnectSchemeName);
            Assert.Equal(AzureADDefaults.CookieScheme, azureADOptions.CookieSchemeName);
            Assert.Equal("https://login.microsoftonline.com", azureADOptions.Instance);
            Assert.Equal("ClientId", azureADOptions.ClientId);
            Assert.Equal("ClientSecret", azureADOptions.ClientSecret);
            Assert.Equal("/signin-oidc", azureADOptions.CallbackPath);
            Assert.Equal("domain.onmicrosoft.com", azureADOptions.Domain);

            var openIdOptionsMonitor = provider.GetService<IOptionsMonitor<OpenIdConnectOptions>>();
            Assert.NotNull(openIdOptionsMonitor);
            var openIdOptions = openIdOptionsMonitor.Get(AzureADDefaults.OpenIdScheme);
            Assert.Equal("ClientId", openIdOptions.ClientId);
            Assert.Equal($"https://login.microsoftonline.com/Common", openIdOptions.Authority);
            Assert.True(openIdOptions.UseTokenLifetime);
            Assert.Equal("/signin-oidc", openIdOptions.CallbackPath);
            Assert.Equal(AzureADDefaults.CookieScheme, openIdOptions.SignInScheme);

            var cookieAuthenticationOptionsMonitor = provider.GetService<IOptionsMonitor<CookieAuthenticationOptions>>();
            Assert.NotNull(cookieAuthenticationOptionsMonitor);
            var cookieAuthenticationOptions = cookieAuthenticationOptionsMonitor.Get(AzureADDefaults.CookieScheme);
            Assert.Equal("/AzureAD/Account/SignIn/AzureAD", cookieAuthenticationOptions.LoginPath);
            Assert.Equal("/AzureAD/Account/SignOut/AzureAD", cookieAuthenticationOptions.LogoutPath);
            Assert.Equal("/AzureAD/Account/AccessDenied", cookieAuthenticationOptions.AccessDeniedPath);
            Assert.Equal(SameSiteMode.None, cookieAuthenticationOptions.Cookie.SameSite);
        }

        [Fact]
        public void AddAzureAD_AllowsOverridingCookiesAndOpenIdConnectSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureAD(o =>
                {
                    o.Instance = "https://login.microsoftonline.com";
                    o.ClientId = "ClientId";
                    o.ClientSecret = "ClientSecret";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.TenantId = "Common";
                });

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, o =>
            {
                o.Authority = "https://overriden.com";
            });

            services.Configure<CookieAuthenticationOptions>(AzureADDefaults.CookieScheme, o =>
            {
                o.AccessDeniedPath = "/Overriden";
            });

            var provider = services.BuildServiceProvider();

            // Assert
            var openIdOptionsMonitor = provider.GetService<IOptionsMonitor<OpenIdConnectOptions>>();
            Assert.NotNull(openIdOptionsMonitor);
            var openIdOptions = openIdOptionsMonitor.Get(AzureADDefaults.OpenIdScheme);
            Assert.Equal("ClientId", openIdOptions.ClientId);
            Assert.Equal($"https://overriden.com", openIdOptions.Authority);

            var cookieAuthenticationOptionsMonitor = provider.GetService<IOptionsMonitor<CookieAuthenticationOptions>>();
            Assert.NotNull(cookieAuthenticationOptionsMonitor);
            var cookieAuthenticationOptions = cookieAuthenticationOptionsMonitor.Get(AzureADDefaults.CookieScheme);
            Assert.Equal("/AzureAD/Account/SignIn/AzureAD", cookieAuthenticationOptions.LoginPath);
            Assert.Equal("/Overriden", cookieAuthenticationOptions.AccessDeniedPath);
        }

        [Fact]
        public void AddAzureAD_RegisteringAddCookiesAndAddOpenIdConnectHasNoImpactOnAzureAAExtensions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddOpenIdConnect()
                .AddCookie()
                .AddAzureAD(o =>
                {
                    o.Instance = "https://login.microsoftonline.com";
                    o.ClientId = "ClientId";
                    o.ClientSecret = "ClientSecret";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.TenantId = "Common";
                });

            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, o =>
            {
                o.Authority = "https://overriden.com";
            });

            services.Configure<CookieAuthenticationOptions>(AzureADDefaults.CookieScheme, o =>
            {
                o.AccessDeniedPath = "/Overriden";
            });

            var provider = services.BuildServiceProvider();

            // Assert
            var openIdOptionsMonitor = provider.GetService<IOptionsMonitor<OpenIdConnectOptions>>();
            Assert.NotNull(openIdOptionsMonitor);
            var openIdOptions = openIdOptionsMonitor.Get(AzureADDefaults.OpenIdScheme);
            Assert.Equal("ClientId", openIdOptions.ClientId);
            Assert.Equal($"https://overriden.com", openIdOptions.Authority);

            var cookieAuthenticationOptionsMonitor = provider.GetService<IOptionsMonitor<CookieAuthenticationOptions>>();
            Assert.NotNull(cookieAuthenticationOptionsMonitor);
            var cookieAuthenticationOptions = cookieAuthenticationOptionsMonitor.Get(AzureADDefaults.CookieScheme);
            Assert.Equal("/AzureAD/Account/SignIn/AzureAD", cookieAuthenticationOptions.LoginPath);
            Assert.Equal("/Overriden", cookieAuthenticationOptions.AccessDeniedPath);
        }

        [Fact]
        public void AddAzureAD_ThrowsForDuplicatedSchemes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureAD(o => { })
                .AddAzureAD(o => { });

            var provider = services.BuildServiceProvider();
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme));

            Assert.Equal("A scheme with the name 'AzureAD' was already added.", exception.Message);
        }

        [Fact]
        public void AddAzureAD_ThrowsWhenOpenIdSchemeIsAlreadyInUse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureAD(o => { })
                .AddAzureAD("Custom", AzureADDefaults.OpenIdScheme, "Cookie", null, o => { });

            var provider = services.BuildServiceProvider();
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();

            var expectedMessage = $"The Open ID Connect scheme 'AzureADOpenID' can't be associated with the Azure Active Directory scheme 'Custom'. " +
                "The Open ID Connect scheme 'AzureADOpenID' is already mapped to the Azure Active Directory scheme 'AzureAD'";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void AddAzureAD_ThrowsWhenCookieSchemeIsAlreadyInUse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureAD(o => { })
                .AddAzureAD("Custom", "OpenID", AzureADDefaults.CookieScheme, null, o => { });

            var provider = services.BuildServiceProvider();
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();

            var expectedMessage = $"The cookie scheme 'AzureADCookie' can't be associated with the Azure Active Directory scheme 'Custom'. " +
                "The cookie scheme 'AzureADCookie' is already mapped to the Azure Active Directory scheme 'AzureAD'";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void AddAzureAD_ThrowsWhenInstanceIsNotSet()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureAD(o => { });

            var provider = services.BuildServiceProvider();
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();

            var expectedMessage = "The 'Instance' option must be provided.";

            // Act & Assert
            var exception = Assert.Throws<OptionsValidationException>(
                () => azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme));

            Assert.Contains(expectedMessage, exception.Failures);
        }

        [Fact]
        public void AddAzureAD_SkipsOptionsValidationForNonAzureCookies()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureAD(o => { })
                .AddCookie("other");

            var provider = services.BuildServiceProvider();
            var cookieAuthOptions = provider.GetService<IOptionsMonitor<CookieAuthenticationOptions>>();

            Assert.NotNull(cookieAuthOptions.Get("other"));
        }

        [Fact]
        public void AddAzureADBearer_AddsAllAuthenticationHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureADBearer(o => { });
            var provider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(provider.GetService<JwtBearerHandler>());
            Assert.NotNull(provider.GetService<PolicySchemeHandler>());
        }

        [Fact]
        public void AddAzureADBearer_ConfiguresAllOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureADBearer(o =>
                {
                    o.Instance = "https://login.microsoftonline.com/";
                    o.ClientId = "ClientId";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.TenantId = "TenantId";
                });
            var provider = services.BuildServiceProvider();

            // Assert
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();
            Assert.NotNull(azureADOptionsMonitor);
            var options = azureADOptionsMonitor.Get(AzureADDefaults.BearerAuthenticationScheme);
            Assert.Equal(AzureADDefaults.JwtBearerAuthenticationScheme, options.JwtBearerSchemeName);
            Assert.Equal("https://login.microsoftonline.com/", options.Instance);
            Assert.Equal("ClientId", options.ClientId);
            Assert.Equal("domain.onmicrosoft.com", options.Domain);

            var bearerOptionsMonitor = provider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            Assert.NotNull(bearerOptionsMonitor);
            var bearerOptions = bearerOptionsMonitor.Get(AzureADDefaults.JwtBearerAuthenticationScheme);
            Assert.Equal("ClientId", bearerOptions.Audience);
            Assert.Equal($"https://login.microsoftonline.com/TenantId", bearerOptions.Authority);
        }

        [Fact]
        public void AddAzureADBearer_CanOverrideJwtBearerOptionsConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddAzureADBearer(o =>
                {
                    o.Instance = "https://login.microsoftonline.com/";
                    o.ClientId = "ClientId";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.TenantId = "TenantId";
                });

            services.Configure<JwtBearerOptions>(AzureADDefaults.JwtBearerAuthenticationScheme, o =>
            {
                o.Audience = "http://overriden.com";
            });

            var provider = services.BuildServiceProvider();

            // Assert
            var bearerOptionsMonitor = provider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            Assert.NotNull(bearerOptionsMonitor);
            var bearerOptions = bearerOptionsMonitor.Get(AzureADDefaults.JwtBearerAuthenticationScheme);
            Assert.Equal("http://overriden.com", bearerOptions.Audience);
            Assert.Equal($"https://login.microsoftonline.com/TenantId", bearerOptions.Authority);
        }

        [Fact]
        public void AddAzureADBearer_RegisteringJwtBearerHasNoImpactOnAzureAAExtensions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            // Act
            services.AddAuthentication()
                .AddJwtBearer()
                .AddAzureADBearer(o =>
                {
                    o.Instance = "https://login.microsoftonline.com/";
                    o.ClientId = "ClientId";
                    o.CallbackPath = "/signin-oidc";
                    o.Domain = "domain.onmicrosoft.com";
                    o.TenantId = "TenantId";
                });

            services.Configure<JwtBearerOptions>(AzureADDefaults.JwtBearerAuthenticationScheme, o =>
            {
                o.Audience = "http://overriden.com";
            });

            var provider = services.BuildServiceProvider();

            // Assert
            var bearerOptionsMonitor = provider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            Assert.NotNull(bearerOptionsMonitor);
            var bearerOptions = bearerOptionsMonitor.Get(AzureADDefaults.JwtBearerAuthenticationScheme);
            Assert.Equal("http://overriden.com", bearerOptions.Audience);
            Assert.Equal($"https://login.microsoftonline.com/TenantId", bearerOptions.Authority);
        }

        [Fact]
        public void AddAzureADBearer_ThrowsForDuplicatedSchemes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADBearer(o => { })
                .AddAzureADBearer(o => { });

            var provider = services.BuildServiceProvider();
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme));

            Assert.Equal("A scheme with the name 'AzureADBearer' was already added.", exception.Message);
        }

        [Fact]
        public void AddAzureADBearer_ThrowsWhenBearerSchemeIsAlreadyInUse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADBearer(o => { })
                .AddAzureADBearer("Custom", AzureADDefaults.JwtBearerAuthenticationScheme, o => { });

            var provider = services.BuildServiceProvider();
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();

            var expectedMessage = $"The JSON Web Token Bearer scheme 'AzureADJwtBearer' can't be associated with the Azure Active Directory scheme 'Custom'. " +
                "The JSON Web Token Bearer scheme 'AzureADJwtBearer' is already mapped to the Azure Active Directory scheme 'AzureADBearer'";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void AddAzureADBearer_ThrowsWhenInstanceIsNotSet()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADBearer(o => { });

            var provider = services.BuildServiceProvider();
            var azureADOptionsMonitor = provider.GetService<IOptionsMonitor<AzureADOptions>>();

            var expectedMessage = "The 'Instance' option must be provided.";

            // Act & Assert
            var exception = Assert.Throws<OptionsValidationException>(
                () => azureADOptionsMonitor.Get(AzureADDefaults.AuthenticationScheme));

            Assert.Contains(expectedMessage, exception.Failures);
        }

        [Fact]
        public void AddAzureADBearer_SkipsOptionsValidationForNonAzureCookies()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new NullLoggerFactory());

            services.AddAuthentication()
                .AddAzureADBearer(o => { })
                .AddJwtBearer("other", o => { });

            var provider = services.BuildServiceProvider();
            var jwtOptions = provider.GetService<IOptionsMonitor<JwtBearerOptions>>();

            Assert.NotNull(jwtOptions.Get("other"));
        }
    }
}
