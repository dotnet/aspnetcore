// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

public class DynamicSchemeTests
{
    [Fact]
    public async Task OptionsAreConfiguredOnce()
    {
        using var host = await CreateHost(s =>
        {
            s.Configure<TestOptions>("One", o => o.Instance = new Singleton());
            s.Configure<TestOptions>("Two", o => o.Instance = new Singleton());
        });
        // Add One scheme
        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("http://example.com/add/One");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transaction = await server.SendAsync("http://example.com/auth/One");
        Assert.Equal("One", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "One"));
        Assert.Equal("1", transaction.FindClaimValue("Count"));

        // Verify option is not recreated
        transaction = await server.SendAsync("http://example.com/auth/One");
        Assert.Equal("One", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "One"));
        Assert.Equal("1", transaction.FindClaimValue("Count"));

        // Add Two scheme
        response = await server.CreateClient().GetAsync("http://example.com/add/Two");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        transaction = await server.SendAsync("http://example.com/auth/Two");
        Assert.Equal("Two", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "Two"));
        Assert.Equal("2", transaction.FindClaimValue("Count"));

        // Verify options are not recreated
        transaction = await server.SendAsync("http://example.com/auth/One");
        Assert.Equal("One", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "One"));
        Assert.Equal("1", transaction.FindClaimValue("Count"));
        transaction = await server.SendAsync("http://example.com/auth/Two");
        Assert.Equal("Two", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "Two"));
        Assert.Equal("2", transaction.FindClaimValue("Count"));
    }

    [Fact]
    public async Task CanAddAndRemoveSchemes()
    {
        using var host = await CreateHost();
        using var server = host.GetTestServer();
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("http://example.com/auth/One"));

        // Add One scheme
        var response = await server.CreateClient().GetAsync("http://example.com/add/One");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transaction = await server.SendAsync("http://example.com/auth/One");
        Assert.Equal("One", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "One"));

        // Add Two scheme
        response = await server.CreateClient().GetAsync("http://example.com/add/Two");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        transaction = await server.SendAsync("http://example.com/auth/Two");
        Assert.Equal("Two", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "Two"));

        // Remove Two
        response = await server.CreateClient().GetAsync("http://example.com/remove/Two");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("http://example.com/auth/Two"));
        transaction = await server.SendAsync("http://example.com/auth/One");
        Assert.Equal("One", transaction.FindClaimValue(ClaimTypes.NameIdentifier, "One"));

        // Remove One
        response = await server.CreateClient().GetAsync("http://example.com/remove/One");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("http://example.com/auth/Two"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("http://example.com/auth/One"));
    }

    public class TestOptions : AuthenticationSchemeOptions
    {
        public Singleton Instance { get; set; }
    }

    public class Singleton
    {
        public static int _count;

        public Singleton()
        {
            _count++;
            Count = _count;
        }

        public int Count { get; }
    }

    private class TestHandler : AuthenticationHandler<TestOptions>
    {
        public TestHandler(IOptionsMonitor<TestOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal();
            var id = new ClaimsIdentity();
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, Scheme.Name, ClaimValueTypes.String, Scheme.Name));
            if (Options.Instance != null)
            {
                id.AddClaim(new Claim("Count", Options.Instance.Count.ToString(CultureInfo.InvariantCulture)));
            }
            principal.AddIdentity(id);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), Scheme.Name)));
        }
    }

    private static async Task<IHost> CreateHost(Action<IServiceCollection> configureServices = null)
    {
        var host = new HostBuilder()
           .ConfigureWebHost(builder =>
               builder.UseTestServer()
                   .Configure(app =>
                   {
                       app.UseAuthentication();
                       app.Use(async (context, next) =>
                       {
                           var req = context.Request;
                           var res = context.Response;
                           if (req.Path.StartsWithSegments(new PathString("/add"), out var remainder))
                           {
                               var name = remainder.Value.Substring(1);
                               var auth = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                               var scheme = new AuthenticationScheme(name, name, typeof(TestHandler));
                               auth.AddScheme(scheme);
                           }
                           else if (req.Path.StartsWithSegments(new PathString("/auth"), out remainder))
                           {
                               var name = (remainder.Value.Length > 0) ? remainder.Value.Substring(1) : null;
                               var result = await context.AuthenticateAsync(name);
                               await res.DescribeAsync(result?.Ticket?.Principal);
                           }
                           else if (req.Path.StartsWithSegments(new PathString("/remove"), out remainder))
                           {
                               var name = remainder.Value.Substring(1);
                               var auth = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                               auth.RemoveScheme(name);
                           }
                           else
                           {
                               await next(context);
                           }
                       });
                   })
                    .ConfigureServices(services =>
                    {
                        configureServices?.Invoke(services);
                        services.AddAuthentication();
                    }))
            .Build();

        await host.StartAsync();
        return host;
    }
}
