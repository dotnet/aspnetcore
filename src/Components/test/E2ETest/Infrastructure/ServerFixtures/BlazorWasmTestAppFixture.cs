// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestServer;
using DevHostServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public class BlazorWasmTestAppFixture<TProgram> : WebHostServerFixture
{
    public readonly bool TestTrimmedOrMultithreadingApps = typeof(ToggleExecutionModeServerFixture<>).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .First(m => m.Key == "Microsoft.AspNetCore.E2ETesting.TestTrimmedOrMultithreadingApps")
        .Value == "true";

    public string Environment { get; set; }
    public string PathBase { get; set; }
    public string ContentRoot { get; private set; }

    public bool RequiresMultithreadingHeaders { get; set; }

    protected override IHost CreateWebHost()
    {
        if (TestTrimmedOrMultithreadingApps)
        {
            var staticFilePath = Path.Combine(AppContext.BaseDirectory, "trimmed-or-threading", typeof(TProgram).Assembly.GetName().Name);
            if (!Directory.Exists(staticFilePath))
            {
                throw new DirectoryNotFoundException($"Test is configured to use trimmed outputs, but trimmed outputs were not found in {staticFilePath}.");
            }

            return CreateStaticWebHost(staticFilePath);
        }

        ContentRoot = FindSampleOrTestSitePath(
            typeof(TProgram).Assembly.FullName);

        var host = "127.0.0.1";
        if (E2ETestOptions.Instance.SauceTest)
        {
            host = E2ETestOptions.Instance.Sauce.HostName;
        }

        var args = new List<string>
            {
                "--urls", $"http://{host}:0",
                "--contentroot", ContentRoot,
                "--pathbase", PathBase,
                "--applicationpath", typeof(TProgram).Assembly.Location,
            };

        if (!string.IsNullOrEmpty(Environment))
        {
            args.Add("--environment");
            args.Add(Environment);
        }

        if (RequiresMultithreadingHeaders || WebAssemblyTestHelper.MultithreadingIsEnabled())
        {
            args.Add("--apply-cop-headers");
        }

        return DevHostServerProgram.BuildWebHost(args.ToArray());
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

            app.Use(async (ctx, next) =>
            {
                // Browser multi-threaded runtime requires cross-origin policy headers to enable SharedArrayBuffer.
                ctx.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
                ctx.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                await next(ctx);
            });

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
