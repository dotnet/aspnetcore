// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

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
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await CreateHostAsync(supportsAuth: true, isEnabled: false));
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
                services.AddAuthentication()
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
