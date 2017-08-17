// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    public class OpenIdConnectConfigurationTests
    {
        [Fact]
        public async Task MetadataAddressIsGeneratedFromAuthorityWhenMissing()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication()
                        .AddCookie()
                        .AddOpenIdConnect(o =>
                    {
                        o.Authority = TestServerBuilder.DefaultAuthority;
                        o.ClientId = Guid.NewGuid().ToString();
                        o.SignInScheme = Guid.NewGuid().ToString();
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(async context =>
                    {
                        var resolver = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                        var handler = await resolver.GetHandlerAsync(context, OpenIdConnectDefaults.AuthenticationScheme) as OpenIdConnectHandler;
                        Assert.Equal($"{TestServerBuilder.DefaultAuthority}/.well-known/openid-configuration", handler.Options.MetadataAddress);
                    });
                });
            var server = new TestServer(builder);
            var transaction = await server.SendAsync(@"https://example.com");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public Task ThrowsWhenSignInSchemeIsSetToSelf()
        {
            return TestConfigurationException<InvalidOperationException>(
                o =>
                {
                    o.SignInScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    o.Authority = TestServerBuilder.DefaultAuthority;
                    o.ClientId = "Test Id";
                    o.ClientSecret = "Test Secret";
                },
                ex => Assert.Contains("cannot be set to itself", ex.Message));
        }

        [Fact]
        public Task ThrowsWhenClientIdIsMissing()
        {
            return TestConfigurationException<ArgumentException>(
                o =>
                {
                    o.SignInScheme = "TestScheme";
                    o.Authority = TestServerBuilder.DefaultAuthority;
                },
                ex => Assert.Equal("ClientId", ex.ParamName));
        }

        [Fact]
        public Task ThrowsWhenAuthorityIsMissing()
        {
            return TestConfigurationException<InvalidOperationException>(
                o =>
                {
                    o.SignInScheme = "TestScheme";
                    o.ClientId = "Test Id";
                    o.CallbackPath = "/";
                },
                ex => Assert.Equal("Provide Authority, MetadataAddress, Configuration, or ConfigurationManager to OpenIdConnectOptions", ex.Message)
            );
        }

        [Fact]
        public Task ThrowsWhenAuthorityIsNotHttps()
        {
            return TestConfigurationException<InvalidOperationException>(
                o =>
                {
                    o.SignInScheme = "TestScheme";
                    o.ClientId = "Test Id";
                    o.MetadataAddress = "http://example.com";
                    o.CallbackPath = "/";
                },
                ex => Assert.Equal("The MetadataAddress or Authority must use HTTPS unless disabled for development by setting RequireHttpsMetadata=false.", ex.Message)
            );
        }

        [Fact]
        public Task ThrowsWhenMetadataAddressIsNotHttps()
        {
            return TestConfigurationException<InvalidOperationException>(
                o =>
                {
                    o.SignInScheme = "TestScheme";
                    o.ClientId = "Test Id";
                    o.MetadataAddress = "http://example.com";
                    o.CallbackPath = "/";
                },
                ex => Assert.Equal("The MetadataAddress or Authority must use HTTPS unless disabled for development by setting RequireHttpsMetadata=false.", ex.Message)
            );
        }

        [Fact]
        public Task ThrowsWhenMaxAgeIsNegative()
        {
            return TestConfigurationException<InvalidOperationException>(
                o =>
                {
                    o.SignInScheme = "TestScheme";
                    o.ClientId = "Test Id";
                    o.Authority = TestServerBuilder.DefaultAuthority;
                    o.MaxAge = TimeSpan.FromSeconds(-1);
                },
                ex => Assert.Equal("MaxAge must not be a negative TimeSpan.", ex.Message)
            );
        }

        private TestServer BuildTestServer(Action<OpenIdConnectOptions> options)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication()
                        .AddCookie()
                        .AddOpenIdConnect(options);
                })
                .Configure(app => app.UseAuthentication());

            return new TestServer(builder);
        }

        private async Task TestConfigurationException<T>(
            Action<OpenIdConnectOptions> options,
            Action<T> verifyException)
            where T : Exception
        {
            var exception = await Assert.ThrowsAsync<T>(() => BuildTestServer(options).SendAsync(@"https://example.com"));
            verifyException(exception);
        }
    }
}
