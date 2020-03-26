// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    [QuarantinedTest]
    public class ServerDeferralTests
    {
        [Fact]
        public async Task ServerDoesNotSupportAuth_NoError()
        {
            using var host = await CreateHostAsync(supportsAuth: false);
            var options = host.Services.GetRequiredService<IOptions<NegotiateOptions>>().Value;
            Assert.False(options.DeferToServer);
            Assert.Null(options.ForwardDefault);
        }

        [Fact]
        public async Task ServerSupportsAuthButDisabled_Error()
        {
            using var host = await CreateHostAsync(supportsAuth: true, isEnabled: false);
            var ex = Assert.Throws<InvalidOperationException>(() => host.Services.GetRequiredService<IOptions<NegotiateOptions>>().Value);
            Assert.Equal("The Negotiate Authentication handler cannot be used on a server that directly supports Windows Authentication."
                        + " Enable Windows Authentication for the server and the Negotiate Authentication handler will defer to it.", ex.Message);
        }

        [Fact]
        public async Task ServerSupportsAuthAndEnabled_Deferred()
        {
            using var host = await CreateHostAsync(supportsAuth: true, isEnabled: true, authScheme: "DeferralScheme");
            var options = host.Services.GetRequiredService<IOptions<NegotiateOptions>>().Value;
            Assert.True(options.DeferToServer);
            Assert.Equal("DeferralScheme", options.ForwardDefault);
        }

        private static async Task<IHost> CreateHostAsync(bool supportsAuth = false, bool isEnabled = false, string authScheme = null)
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                        .AddNegotiate();

                    if (supportsAuth)
                    {
                        services.AddSingleton<IServerIntegratedAuth>(new ServerIntegratedAuth()
                        {
                            IsEnabled = isEnabled,
                            AuthenticationScheme = authScheme,
                        });
                    }
                })
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.Configure(app =>
                    {
                        app.UseAuthentication();
                    });
                });

            return await builder.StartAsync();
        }
    }
}
