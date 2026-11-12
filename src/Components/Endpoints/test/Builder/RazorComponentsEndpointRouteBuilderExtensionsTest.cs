// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Microsoft.AspNetCore.Builder;

public class RazorComponentsEndpointRouteBuilderExtensionsTest
{
    [Fact]
    public async Task MapRazorComponents_MatchesBrowserToolsHotReloadSettingsWithMultipleRoots()
    {
        await AssertMappingsAsync(BrowserToolsHotReloadSettings.Path, int.MaxValue, async (services, application, prefix, host) =>
        {
            var context = CreateHttpContext(services, prefix + BrowserToolsHotReloadSettings.Path, host);

            await application(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
            Assert.Equal("no-store", context.Response.Headers.CacheControl);
            Assert.Equal("""{ "hotReload": false }""", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
            AssertHost(context, host);
        });
    }

    [Fact]
    public async Task MapRazorComponents_MatchesOpaqueRedirectionWithMultipleRoots()
    {
        await AssertMappingsAsync("/_framework/opaque-redirect", 0, async (services, application, prefix, host) =>
        {
            var invalidRequest = CreateHttpContext(services, prefix + "/_framework/opaque-redirect?url=x", host);

            await application(invalidRequest);

            Assert.Equal(StatusCodes.Status400BadRequest, invalidRequest.Response.StatusCode);
            AssertHost(invalidRequest, host);

            var redirectUrl = OpaqueRedirection.CreateProtectedRedirectionUrl(invalidRequest, "/destination");
            var validRequest = CreateHttpContext(services, $"{prefix}/{redirectUrl}", host);

            await application(validRequest);

            Assert.Equal(StatusCodes.Status302Found, validRequest.Response.StatusCode);
            Assert.Equal("/destination", validRequest.Response.Headers.Location);
            AssertHost(validRequest, host);
        });
    }

    private static async Task AssertMappingsAsync(
        string frameworkPath,
        int frameworkEndpointOrder,
        Func<IServiceProvider, RequestDelegate, string, string, Task> assertion)
    {
        var results = new List<(string Scenario, Exception Error)>();
        foreach (var scenario in Enum.GetValues<MappingScenario>())
        {
            foreach (var reverse in new[] { false, true })
            {
                using var services = CreateServices();
                var application = CreateApplication(services, scenario, reverse);
                var prefixes = scenario switch
                {
                    MappingScenario.SamePrefix => new[] { "/group" },
                    MappingScenario.SeparatePrefixes => ["/a", "/b"],
                    _ => [""],
                };
                var hosts = scenario is MappingScenario.Hosts or MappingScenario.GroupHosts
                    ? new[] { "a.example", "b.example" }
                    : ["localhost"];

                foreach (var prefix in prefixes)
                {
                    foreach (var host in hosts)
                    {
                        var error = await Record.ExceptionAsync(async () =>
                        {
                            await assertion(services, application, prefix, host);

                            var post = CreateHttpContext(services, prefix + frameworkPath, host);
                            post.Request.Method = HttpMethods.Post;
                            await application(post);
                            Assert.Equal(StatusCodes.Status405MethodNotAllowed, post.Response.StatusCode);

                            if (scenario is MappingScenario.Hosts or MappingScenario.GroupHosts)
                            {
                                var unmatchedHost = CreateHttpContext(services, prefix + frameworkPath, "other.example");
                                await application(unmatchedHost);
                                Assert.Equal(StatusCodes.Status404NotFound, unmatchedHost.Response.StatusCode);
                            }
                        });
                        results.Add(($"{scenario}, reverse={reverse}, prefix={prefix}, host={host}", error));
                    }
                }
            }
        }

        Assert.All(results, result => Assert.True(result.Error is null, $"{result.Scenario}: {result.Error}"));

        foreach (var order in new[] { -1, frameworkEndpointOrder })
        {
            using var services = CreateServices();
            var application = new ApplicationBuilder(services);
            application.UseRouting();
            application.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorComponents<FirstApp>();
                endpoints.MapRazorComponents<SecondApp>();
                endpoints.MapGet(frameworkPath, context =>
                {
                    context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                    return Task.CompletedTask;
                }).Add(builder => ((RouteEndpointBuilder)builder).Order = order);
            });
            var pipeline = application.Build();
            var context = CreateHttpContext(services, frameworkPath, "localhost");

            if (order == frameworkEndpointOrder)
            {
                var exception = await Record.ExceptionAsync(() => pipeline(context));
                Assert.NotNull(exception);
                Assert.Equal("Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException", exception.GetType().FullName);
            }
            else
            {
                await pipeline(context);
                Assert.Equal(StatusCodes.Status418ImATeapot, context.Response.StatusCode);
            }
        }
    }

    private static void AssertHost(HttpContext context, string host)
    {
        if (host != "localhost")
        {
            Assert.Equal(host, Assert.Single(context.GetEndpoint()!.Metadata.GetRequiredMetadata<IHostMetadata>().Hosts));
        }
    }

    private static RequestDelegate CreateApplication(IServiceProvider services, MappingScenario scenario, bool reverse)
    {
        var application = new ApplicationBuilder(services);
        application.UseRouting();
        application.UseEndpoints(endpoints =>
        {
            (IEndpointRouteBuilder first, IEndpointRouteBuilder second) = scenario switch
            {
                MappingScenario.EmptyGroup => (endpoints, endpoints.MapGroup("")),
                MappingScenario.SamePrefix => (endpoints.MapGroup("/group"), endpoints.MapGroup("/group")),
                MappingScenario.SeparatePrefixes => (endpoints.MapGroup("/a"), endpoints.MapGroup("/b")),
                MappingScenario.GroupHosts => (endpoints.MapGroup("").RequireHost("a.example"), endpoints.MapGroup("").RequireHost("b.example")),
                _ => (endpoints, endpoints),
            };

            if (reverse)
            {
                MapSecond();
                MapFirst();
            }
            else
            {
                MapFirst();
                MapSecond();
            }

            void MapFirst()
            {
                var builder = first.MapRazorComponents<FirstApp>();
                if (scenario == MappingScenario.Hosts)
                {
                    builder.RequireHost("a.example");
                }
            }

            void MapSecond()
            {
                if (scenario == MappingScenario.SingleRoot)
                {
                    return;
                }

                var builder = second.MapRazorComponents<SecondApp>();
                if (scenario == MappingScenario.Hosts)
                {
                    builder.RequireHost("b.example");
                }
            }
        });

        return application.Build();
    }

    private static ServiceProvider CreateServices()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Development);
        environment.SetupGet(environment => environment.ApplicationName).Returns(typeof(FirstApp).Assembly.GetName().Name);
        environment.SetupGet(environment => environment.ContentRootFileProvider).Returns(new NullFileProvider());
        environment.SetupGet(environment => environment.WebRootFileProvider).Returns(new NullFileProvider());

        var services = new ServiceCollection();
        services.AddSingleton(environment.Object);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["DOTNET_WATCH"] = "0",
        }).Build());
        services.AddSingleton(new DiagnosticListener("Test"));
        services.AddRouting();
        services.AddRazorComponents();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider services, string path, string host)
    {
        var uri = new Uri($"http://localhost{path}");
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Host = new HostString(host);
        context.Request.Path = uri.AbsolutePath;
        context.Request.QueryString = new QueryString(uri.Query);
        context.Response.Body = new MemoryStream();

        return context;
    }

    private sealed class FirstApp : ComponentBase;
    private sealed class SecondApp : ComponentBase;

    private enum MappingScenario
    {
        SingleRoot,
        SameBuilder,
        EmptyGroup,
        SamePrefix,
        SeparatePrefixes,
        Hosts,
        GroupHosts,
    }
}
