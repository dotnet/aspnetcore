// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

public class OpenIdConnectConfigurationTests
{
    private void ConfigureDefaults(OpenIdConnectOptions o)
    {
        o.Authority = TestServerBuilder.DefaultAuthority;
        o.ClientId = "Test Id";
        o.ClientSecret = "Test Secret";
        o.SignInScheme = "auth1";
    }

    [Fact]
    public async Task CanForwardDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler>("auth1", "auth1");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
        });

        var forwardDefault = new TestHandler();
        services.AddSingleton(forwardDefault);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        Assert.Equal(0, forwardDefault.AuthenticateCount);
        Assert.Equal(0, forwardDefault.ForbidCount);
        Assert.Equal(0, forwardDefault.ChallengeCount);
        Assert.Equal(0, forwardDefault.SignInCount);
        Assert.Equal(0, forwardDefault.SignOutCount);

        await context.AuthenticateAsync();
        Assert.Equal(1, forwardDefault.AuthenticateCount);

        await context.ForbidAsync();
        Assert.Equal(1, forwardDefault.ForbidCount);

        await context.ChallengeAsync();
        Assert.Equal(1, forwardDefault.ChallengeCount);

        await context.SignOutAsync();
        Assert.Equal(1, forwardDefault.SignOutCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
    }

    [Fact]
    public async Task ForwardSignInThrows()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler2>("auth1", "auth1");
            o.AddScheme<TestHandler>("specific", "specific");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardSignOut = "specific";
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
    }

    [Fact]
    public async Task ForwardSignOutWinsOverDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler2>("auth1", "auth1");
            o.AddScheme<TestHandler>("specific", "specific");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardSignOut = "specific";
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await context.SignOutAsync();
        Assert.Equal(1, specific.SignOutCount);
        Assert.Equal(0, specific.AuthenticateCount);
        Assert.Equal(0, specific.ForbidCount);
        Assert.Equal(0, specific.ChallengeCount);
        Assert.Equal(0, specific.SignInCount);

        Assert.Equal(0, forwardDefault.AuthenticateCount);
        Assert.Equal(0, forwardDefault.ForbidCount);
        Assert.Equal(0, forwardDefault.ChallengeCount);
        Assert.Equal(0, forwardDefault.SignInCount);
        Assert.Equal(0, forwardDefault.SignOutCount);
    }

    [Fact]
    public async Task ForwardForbidWinsOverDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler2>("auth1", "auth1");
            o.AddScheme<TestHandler>("specific", "specific");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardForbid = "specific";
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await context.ForbidAsync();
        Assert.Equal(0, specific.SignOutCount);
        Assert.Equal(0, specific.AuthenticateCount);
        Assert.Equal(1, specific.ForbidCount);
        Assert.Equal(0, specific.ChallengeCount);
        Assert.Equal(0, specific.SignInCount);

        Assert.Equal(0, forwardDefault.AuthenticateCount);
        Assert.Equal(0, forwardDefault.ForbidCount);
        Assert.Equal(0, forwardDefault.ChallengeCount);
        Assert.Equal(0, forwardDefault.SignInCount);
        Assert.Equal(0, forwardDefault.SignOutCount);
    }

    [Fact]
    public async Task ForwardAuthenticateWinsOverDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler2>("auth1", "auth1");
            o.AddScheme<TestHandler>("specific", "specific");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardAuthenticate = "specific";
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await context.AuthenticateAsync();
        Assert.Equal(0, specific.SignOutCount);
        Assert.Equal(1, specific.AuthenticateCount);
        Assert.Equal(0, specific.ForbidCount);
        Assert.Equal(0, specific.ChallengeCount);
        Assert.Equal(0, specific.SignInCount);

        Assert.Equal(0, forwardDefault.AuthenticateCount);
        Assert.Equal(0, forwardDefault.ForbidCount);
        Assert.Equal(0, forwardDefault.ChallengeCount);
        Assert.Equal(0, forwardDefault.SignInCount);
        Assert.Equal(0, forwardDefault.SignOutCount);
    }

    [Fact]
    public async Task ForwardChallengeWinsOverDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler>("specific", "specific");
            o.AddScheme<TestHandler2>("auth1", "auth1");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardChallenge = "specific";
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await context.ChallengeAsync();
        Assert.Equal(0, specific.SignOutCount);
        Assert.Equal(0, specific.AuthenticateCount);
        Assert.Equal(0, specific.ForbidCount);
        Assert.Equal(1, specific.ChallengeCount);
        Assert.Equal(0, specific.SignInCount);

        Assert.Equal(0, forwardDefault.AuthenticateCount);
        Assert.Equal(0, forwardDefault.ForbidCount);
        Assert.Equal(0, forwardDefault.ChallengeCount);
        Assert.Equal(0, forwardDefault.SignInCount);
        Assert.Equal(0, forwardDefault.SignOutCount);
    }

    [Fact]
    public async Task ForwardSelectorWinsOverDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler2>("auth1", "auth1");
            o.AddScheme<TestHandler3>("selector", "selector");
            o.AddScheme<TestHandler>("specific", "specific");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardDefaultSelector = _ => "selector";
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);
        var selector = new TestHandler3();
        services.AddSingleton(selector);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await context.AuthenticateAsync();
        Assert.Equal(1, selector.AuthenticateCount);

        await context.ForbidAsync();
        Assert.Equal(1, selector.ForbidCount);

        await context.ChallengeAsync();
        Assert.Equal(1, selector.ChallengeCount);

        await context.SignOutAsync();
        Assert.Equal(1, selector.SignOutCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));

        Assert.Equal(0, forwardDefault.AuthenticateCount);
        Assert.Equal(0, forwardDefault.ForbidCount);
        Assert.Equal(0, forwardDefault.ChallengeCount);
        Assert.Equal(0, forwardDefault.SignInCount);
        Assert.Equal(0, forwardDefault.SignOutCount);
        Assert.Equal(0, specific.AuthenticateCount);
        Assert.Equal(0, specific.ForbidCount);
        Assert.Equal(0, specific.ChallengeCount);
        Assert.Equal(0, specific.SignInCount);
        Assert.Equal(0, specific.SignOutCount);
    }

    [Fact]
    public async Task NullForwardSelectorUsesDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler2>("auth1", "auth1");
            o.AddScheme<TestHandler3>("selector", "selector");
            o.AddScheme<TestHandler>("specific", "specific");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardDefaultSelector = _ => null;
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);
        var selector = new TestHandler3();
        services.AddSingleton(selector);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await context.AuthenticateAsync();
        Assert.Equal(1, forwardDefault.AuthenticateCount);

        await context.ForbidAsync();
        Assert.Equal(1, forwardDefault.ForbidCount);

        await context.ChallengeAsync();
        Assert.Equal(1, forwardDefault.ChallengeCount);

        await context.SignOutAsync();
        Assert.Equal(1, forwardDefault.SignOutCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));

        Assert.Equal(0, selector.AuthenticateCount);
        Assert.Equal(0, selector.ForbidCount);
        Assert.Equal(0, selector.ChallengeCount);
        Assert.Equal(0, selector.SignInCount);
        Assert.Equal(0, selector.SignOutCount);
        Assert.Equal(0, specific.AuthenticateCount);
        Assert.Equal(0, specific.ForbidCount);
        Assert.Equal(0, specific.ChallengeCount);
        Assert.Equal(0, specific.SignInCount);
        Assert.Equal(0, specific.SignOutCount);
    }

    [Fact]
    public async Task SpecificForwardWinsOverSelectorAndDefault()
    {
        var services = new ServiceCollection().ConfigureAuthTestServices();
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = OpenIdConnectDefaults.AuthenticationScheme;
            o.AddScheme<TestHandler2>("auth1", "auth1");
            o.AddScheme<TestHandler3>("selector", "selector");
            o.AddScheme<TestHandler>("specific", "specific");
        })
        .AddOpenIdConnect(o =>
        {
            ConfigureDefaults(o);
            o.ForwardDefault = "auth1";
            o.ForwardDefaultSelector = _ => "selector";
            o.ForwardAuthenticate = "specific";
            o.ForwardChallenge = "specific";
            o.ForwardSignIn = "specific";
            o.ForwardSignOut = "specific";
            o.ForwardForbid = "specific";
        });

        var specific = new TestHandler();
        services.AddSingleton(specific);
        var forwardDefault = new TestHandler2();
        services.AddSingleton(forwardDefault);
        var selector = new TestHandler3();
        services.AddSingleton(selector);

        var sp = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.RequestServices = sp;

        await context.AuthenticateAsync();
        Assert.Equal(1, specific.AuthenticateCount);

        await context.ForbidAsync();
        Assert.Equal(1, specific.ForbidCount);

        await context.ChallengeAsync();
        Assert.Equal(1, specific.ChallengeCount);

        await context.SignOutAsync();
        Assert.Equal(1, specific.SignOutCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));

        Assert.Equal(0, forwardDefault.AuthenticateCount);
        Assert.Equal(0, forwardDefault.ForbidCount);
        Assert.Equal(0, forwardDefault.ChallengeCount);
        Assert.Equal(0, forwardDefault.SignInCount);
        Assert.Equal(0, forwardDefault.SignOutCount);
        Assert.Equal(0, selector.AuthenticateCount);
        Assert.Equal(0, selector.ForbidCount);
        Assert.Equal(0, selector.ChallengeCount);
        Assert.Equal(0, selector.SignInCount);
        Assert.Equal(0, selector.SignOutCount);
    }

    [Fact]
    public async Task MetadataAddressIsGeneratedFromAuthorityWhenMissing()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Run(async context =>
                        {
                            var resolver = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                            var handler = await resolver.GetHandlerAsync(context, OpenIdConnectDefaults.AuthenticationScheme) as OpenIdConnectHandler;
                            Assert.Equal($"{TestServerBuilder.DefaultAuthority}/.well-known/openid-configuration", handler.Options.MetadataAddress);
                        });
                    })
                    .UseTestServer();
            })
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
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();
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
        return TestConfigurationException<ArgumentNullException>(
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
                o.Authority = "http://example.com";
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
        return TestConfigurationException<ArgumentOutOfRangeException>(
            o =>
            {
                o.SignInScheme = "TestScheme";
                o.ClientId = "Test Id";
                o.Authority = TestServerBuilder.DefaultAuthority;
                o.MaxAge = TimeSpan.FromSeconds(-1);
            },
            ex => Assert.StartsWith("The value must not be a negative TimeSpan.", ex.Message)
        );
    }

    private TestServer BuildTestServer(Action<OpenIdConnectOptions> options)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication()
                        .AddCookie()
                        .AddOpenIdConnect(options);
                })
                .Configure(app => app.UseAuthentication()))
            .Build();
        host.Start();
        return host.GetTestServer();
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
