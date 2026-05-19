// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Authentication;

public class AuthenticationMiddlewareTests
{
    [Fact]
    public async Task OnlyInvokesCanHandleRequestHandlers()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                    })
                    .ConfigureServices(services => services.AddAuthentication(o =>
                    {
                        o.AddScheme("Skip", s =>
                        {
                            s.HandlerType = typeof(SkipHandler);
                        });
                        // Won't get hit since CanHandleRequests is false
                        o.AddScheme("throws", s =>
                        {
                            s.HandlerType = typeof(ThrowsHandler);
                        });
                        o.AddScheme("607", s =>
                        {
                            s.HandlerType = typeof(SixOhSevenHandler);
                        });
                        // Won't get run since 607 will finish
                        o.AddScheme("305", s =>
                        {
                            s.HandlerType = typeof(ThreeOhFiveHandler);
                        });
                    })))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("http://example.com/");
        Assert.Equal(607, (int)response.StatusCode);
    }

    [Fact]
    public async Task IAuthenticateResultFeature_SetOnSuccessfulAuthenticate()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService.Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "custom"))));
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider.Setup(p => p.GetDefaultAuthenticateSchemeAsync())
            .Returns(Task.FromResult(new AuthenticationScheme("custom", "custom", typeof(JwtBearerHandler))));
        var middleware = new AuthenticationMiddleware(c => Task.CompletedTask, schemeProvider.Object);
        var context = GetHttpContext(authenticationService: authenticationService.Object);

        // Act
        await middleware.Invoke(context);

        // Assert
        var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
        Assert.NotNull(authenticateResultFeature);
        Assert.NotNull(authenticateResultFeature.AuthenticateResult);
        Assert.True(authenticateResultFeature.AuthenticateResult.Succeeded);
        Assert.Same(context.User, authenticateResultFeature.AuthenticateResult.Principal);
    }

    [Fact]
    public async Task IAuthenticateResultFeature_NotSetOnUnsuccessfulAuthenticate()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService.Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult(AuthenticateResult.Fail("not authenticated")));
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider.Setup(p => p.GetDefaultAuthenticateSchemeAsync())
            .Returns(Task.FromResult(new AuthenticationScheme("custom", "custom", typeof(JwtBearerHandler))));
        var middleware = new AuthenticationMiddleware(c => Task.CompletedTask, schemeProvider.Object);
        var context = GetHttpContext(authenticationService: authenticationService.Object);

        // Act
        await middleware.Invoke(context);

        // Assert
        var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
        Assert.Null(authenticateResultFeature);
    }

    [Fact]
    public async Task IAuthenticateResultFeature_NullResultWhenUserSetAfter()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService.Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "custom"))));
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider.Setup(p => p.GetDefaultAuthenticateSchemeAsync())
            .Returns(Task.FromResult(new AuthenticationScheme("custom", "custom", typeof(JwtBearerHandler))));
        var middleware = new AuthenticationMiddleware(c => Task.CompletedTask, schemeProvider.Object);
        var context = GetHttpContext(authenticationService: authenticationService.Object);

        // Act
        await middleware.Invoke(context);

        // Assert
        var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
        Assert.NotNull(authenticateResultFeature);
        Assert.NotNull(authenticateResultFeature.AuthenticateResult);
        Assert.True(authenticateResultFeature.AuthenticateResult.Succeeded);
        Assert.Same(context.User, authenticateResultFeature.AuthenticateResult.Principal);

        context.User = new ClaimsPrincipal();
        Assert.Null(authenticateResultFeature.AuthenticateResult);
    }

    [Fact]
    public async Task IAuthenticateResultFeature_SettingResultSetsUser()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService.Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "custom"))));
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>();
        schemeProvider.Setup(p => p.GetDefaultAuthenticateSchemeAsync())
            .Returns(Task.FromResult(new AuthenticationScheme("custom", "custom", typeof(JwtBearerHandler))));
        var middleware = new AuthenticationMiddleware(c => Task.CompletedTask, schemeProvider.Object);
        var context = GetHttpContext(authenticationService: authenticationService.Object);

        // Act
        await middleware.Invoke(context);

        // Assert
        var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
        Assert.NotNull(authenticateResultFeature);
        Assert.NotNull(authenticateResultFeature.AuthenticateResult);
        Assert.True(authenticateResultFeature.AuthenticateResult.Succeeded);
        Assert.Same(context.User, authenticateResultFeature.AuthenticateResult.Principal);

        var newTicket = new AuthenticationTicket(new ClaimsPrincipal(), "");
        authenticateResultFeature.AuthenticateResult = AuthenticateResult.Success(newTicket);
        Assert.Same(context.User, newTicket.Principal);
    }

    [Fact]
    public async Task WebApplicationBuilder_RegistersAuthenticationAndAuthorizationMiddlewares()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Authentication:Schemes:Bearer:ValidIssuer", "SomeIssuer"),
            new KeyValuePair<string, string>("Authentication:Schemes:Bearer:ValidAudiences:0", "https://localhost:5001")
        });
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication().AddJwtBearer();
        await using var app = builder.Build();

        // Authentication middleware isn't registered until application
        // is built on startup
        Assert.False(app.Properties.ContainsKey("__AuthenticationMiddlewareSet"));
        Assert.False(app.Properties.ContainsKey("__AuthorizationMiddlewareSet"));

        await app.StartAsync();

        Assert.True(app.Properties.ContainsKey("__AuthenticationMiddlewareSet"));
        Assert.True(app.Properties.ContainsKey("__AuthorizationMiddlewareSet"));

        var options = app.Services.GetService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.Equal("SomeIssuer", options.TokenValidationParameters.ValidIssuer);
        Assert.Equal(new[] { "https://localhost:5001" }, options.TokenValidationParameters.ValidAudiences);
    }

    [Fact]
    public async Task WebApplicationBuilder_OnlyRegistersMiddlewareWithSupportedServices()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthentication().AddJwtBearer();
        await using var app = builder.Build();

        Assert.False(app.Properties.ContainsKey("__AuthenticationMiddlewareSet"));
        Assert.False(app.Properties.ContainsKey("__AuthorizationMiddlewareSet"));

        await app.StartAsync();

        Assert.True(app.Properties.ContainsKey("__AuthenticationMiddlewareSet"));
        Assert.False(app.Properties.ContainsKey("__AuthorizationMiddlewareSet"));
    }

    private HttpContext GetHttpContext(
        Action<IServiceCollection> registerServices = null,
        IAuthenticationService authenticationService = null)
    {
        // ServiceProvider
        var serviceCollection = new ServiceCollection();

        authenticationService = authenticationService ?? Mock.Of<IAuthenticationService>();

        serviceCollection.AddSingleton(authenticationService);
        serviceCollection.AddOptions();
        serviceCollection.AddLogging();
        serviceCollection.AddAuthentication();
        serviceCollection.AddSingleton<IConfiguration>(new ConfigurationManager());
        registerServices?.Invoke(serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        //// HttpContext
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;

        return httpContext;
    }

    private class ThreeOhFiveHandler : StatusCodeHandler
    {
        public ThreeOhFiveHandler() : base(305) { }
    }

    private class SixOhSevenHandler : StatusCodeHandler
    {
        public SixOhSevenHandler() : base(607) { }
    }

    private class SevenOhSevenHandler : StatusCodeHandler
    {
        public SevenOhSevenHandler() : base(707) { }
    }

    private class StatusCodeHandler : IAuthenticationRequestHandler
    {
        private HttpContext _context;
        private readonly int _code;

        public StatusCodeHandler(int code)
        {
            _code = code;
        }

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HandleRequestAsync()
        {
            _context.Response.StatusCode = _code;
            return Task.FromResult(true);
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _context = context;
            return Task.FromResult(0);
        }

        public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task SignOutAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }
    }

    private class ThrowsHandler : IAuthenticationHandler
    {
        private HttpContext _context;

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HandleRequestAsync()
        {
            throw new NotImplementedException();
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _context = context;
            return Task.FromResult(0);
        }

        public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task SignOutAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }
    }

    private class SkipHandler : IAuthenticationRequestHandler
    {
        private HttpContext _context;

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HandleRequestAsync()
        {
            return Task.FromResult(false);
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _context = context;
            return Task.FromResult(0);
        }

        public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task SignOutAsync(AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }
    }
}
