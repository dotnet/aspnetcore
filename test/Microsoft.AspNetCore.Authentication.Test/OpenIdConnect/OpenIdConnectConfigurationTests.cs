// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Tests.OpenIdConnect.Infrastructre;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Tests.OpenIdConnect
{
    public class OpenIdConnectConfigurationTests
    {
        [Fact]
        public void MetadataAddressIsGeneratedFromAuthorityWhenMissing()
        {
            var options = new OpenIdConnectOptions
            {
                Authority = TestDefaultValues.DefaultAuthority,
                ClientId = Guid.NewGuid().ToString(),
                SignInScheme = Guid.NewGuid().ToString()
            };

            BuildTestServer(options);

            Assert.Equal($"{options.Authority}/.well-known/openid-configuration", options.MetadataAddress);
        }

        public void ThrowsWhenSignInSchemeIsMissing()
        {
            TestConfigurationException<ArgumentException>(
                new OpenIdConnectOptions
                {
                    Authority = TestDefaultValues.DefaultAuthority,
                    ClientId = Guid.NewGuid().ToString()
                },
                ex => Assert.Equal("SignInScheme", ex.ParamName));
        }

        [Fact]
        public void ThrowsWhenClientIdIsMissing()
        {
            TestConfigurationException<ArgumentException>(
                new OpenIdConnectOptions
                {
                    SignInScheme = "TestScheme",
                    Authority = TestDefaultValues.DefaultAuthority,
                },
                ex => Assert.Equal("ClientId", ex.ParamName));
        }

        [Fact]
        public void ThrowsWhenAuthorityIsMissing()
        {
            TestConfigurationException<InvalidOperationException>(
                new OpenIdConnectOptions
                {
                    SignInScheme = "TestScheme",
                    ClientId = "Test Id",
                },
                ex => Assert.Equal("Provide Authority, MetadataAddress, Configuration, or ConfigurationManager to OpenIdConnectOptions", ex.Message)
            );
        }

        [Fact]
        public void ThrowsWhenAuthorityIsNotHttps()
        {
            TestConfigurationException<InvalidOperationException>(
                new OpenIdConnectOptions
                {
                    SignInScheme = "TestScheme",
                    ClientId = "Test Id",
                    Authority = "http://example.com"
                },
                ex => Assert.Equal("The MetadataAddress or Authority must use HTTPS unless disabled for development by setting RequireHttpsMetadata=false.", ex.Message)
            );
        }

        [Fact]
        public void ThrowsWhenMetadataAddressIsNotHttps()
        {
            TestConfigurationException<InvalidOperationException>(
                new OpenIdConnectOptions
                {
                    SignInScheme = "TestScheme",
                    ClientId = "Test Id",
                    MetadataAddress = "http://example.com"
                },
                ex => Assert.Equal("The MetadataAddress or Authority must use HTTPS unless disabled for development by setting RequireHttpsMetadata=false.", ex.Message)
            );
        }

        private TestServer BuildTestServer(OpenIdConnectOptions options)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddAuthentication())
                .Configure(app => app.UseOpenIdConnectAuthentication(options));

            return new TestServer(builder);
        }

        private void TestConfigurationException<T>(
            OpenIdConnectOptions options,
            Action<T> verifyException)
            where T : Exception
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddAuthentication())
                .Configure(app => app.UseOpenIdConnectAuthentication(options));

            var exception = Assert.Throws<T>(() =>
            {
                new TestServer(builder);
            });

            verifyException(exception);
        }
    }
}
