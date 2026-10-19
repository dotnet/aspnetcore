// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Gateway;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public class BlazorWasmTestAppFixture<TProgram> : WebHostServerFixture
{
    public readonly bool TestTrimmedApps = typeof(ToggleExecutionModeServerFixture<>).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(m => m.Key == "Microsoft.AspNetCore.E2ETesting.TestTrimmedApps")
        .Value == "true";

    public string Environment { get; set; }
    public string PathBase { get; set; }
    public string ContentRoot { get; private set; }

    protected override IHost CreateWebHost()
    {
        if (TestTrimmedApps)
        {
            var staticFilePath = Path.Combine(AppContext.BaseDirectory, "trimmed", typeof(TProgram).Assembly.GetName().Name);
            if (!Directory.Exists(staticFilePath))
            {
                throw new DirectoryNotFoundException($"Test is configured to use trimmed outputs, but trimmed outputs were not found in {staticFilePath}.");
            }

            return CreateStaticWebHost(staticFilePath);
        }

        var clientAssemblyPath = typeof(TProgram).Assembly.Location;
        ContentRoot = FindSampleOrTestSitePath(typeof(TProgram).Assembly.FullName);
        var indexHtmlPath = Path.Combine(ContentRoot, "wwwroot", "index.html");
        var runtimeManifestPath = Path.ChangeExtension(clientAssemblyPath, ".staticwebassets.runtime.json");
        var endpointsManifestPath = Path.ChangeExtension(clientAssemblyPath, ".staticwebassets.endpoints.json");
        var clientAssemblyExists = File.Exists(clientAssemblyPath);
        var contentRootExists = Directory.Exists(ContentRoot);
        var indexHtmlExists = File.Exists(indexHtmlPath);
        var runtimeManifestExists = File.Exists(runtimeManifestPath);
        var endpointsManifestExists = File.Exists(endpointsManifestPath);

        if (!clientAssemblyExists ||
            !contentRootExists ||
            !indexHtmlExists ||
            !runtimeManifestExists ||
            !endpointsManifestExists)
        {
            throw new InvalidOperationException(
                $"""
                The Blazor WebAssembly E2E test app is not ready to start.
                Client assembly: '{clientAssemblyPath}' ({(clientAssemblyExists ? "found" : "missing")})
                Content root: '{ContentRoot}' ({(contentRootExists ? "found" : "missing")})
                Entry point: '{indexHtmlPath}' ({(indexHtmlExists ? "found" : "missing")})
                Runtime manifest: '{runtimeManifestPath}' ({(runtimeManifestExists ? "found" : "missing")})
                Endpoints manifest: '{endpointsManifestPath}' ({(endpointsManifestExists ? "found" : "missing")})

                Rebuild the E2E project and referenced test apps before rerunning the test with --no-build:
                  dotnet build src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj --no-restore
                A --no-dependencies build can copy existing dependency outputs, but it does not rebuild referenced client apps.
                Do not use it when referenced app outputs may be stale or missing.
                """);
        }

        var host = "127.0.0.1";
        if (E2ETestOptions.Instance.SauceTest)
        {
            host = E2ETestOptions.Instance.Sauce.HostName;
        }

        var args = new List<string>
            {
                "--urls", $"http://{host}:0",
                "--contentroot", ContentRoot,
                "--Gateway:PathBase", PathBase,
                "--staticWebAssets", runtimeManifestPath,
                "--ClientApps:app:EndpointsManifest", endpointsManifestPath,
                "--ClientApps:app:PathPrefix", "",
            };

        if (!string.IsNullOrEmpty(Environment))
        {
            args.Add("--environment");
            args.Add(Environment);
        }

        var app = BlazorGateway.BuildWebHost(args.ToArray());
        app.MapFallbackToFile("index.html");
        return app;
    }

    private IHost CreateStaticWebHost(string contentRoot)
    {
        var host = "127.0.0.1";
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder => webHostBuilder
                .UseKestrel()
                .UseContentRoot(contentRoot)
                .UseStartup(_ => new StaticSiteStartup { PathBase = PathBase })
                .UseUrls($"http://{host}:0"))
            .ConfigureLogging((hostingContext, logging) => logging.AddConsole())
            .Build();
    }

    private class StaticSiteStartup
    {
        public string PathBase { get; init; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (!string.IsNullOrEmpty(PathBase))
            {
                app.UsePathBase(PathBase);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
