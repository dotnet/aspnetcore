// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Testing;
using DevServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace TestServer;

public class Program
{
    private static int nextPortNumber = 5001;

    public static async Task Main(string[] args)
    {
        var createIndividualHosts = new Dictionary<string, (IHost host, string basePath)>
        {
            ["Client authentication"] = (BuildWebHost<AuthenticationStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["Remote client authentication"] = (BuildWebHost<RemoteAuthenticationStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["Server authentication"] = (BuildWebHost<ServerAuthenticationStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["CORS (WASM)"] = (BuildWebHost<CorsStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["Prerendering (Server-side)"] = (BuildWebHost<PrerenderedStartup>(CreateAdditionalArgs(args)), "/prerendered"),
            ["Razor Component Endpoints"] = (BuildWebHost<RazorComponentEndpointsStartup<Components.TestServer.RazorComponents.App>>(CreateAdditionalArgs(args)), "/subdir"),
            ["Deferred component content (Server-side)"] = (BuildWebHost<DeferredComponentContentStartup>(CreateAdditionalArgs(args)), "/deferred-component-content"),
            ["Locked navigation (Server-side)"] = (BuildWebHost<LockedNavigationStartup>(CreateAdditionalArgs(args)), "/locked-navigation"),
            ["Client-side with fallback"] = (BuildWebHost<StartupWithMapFallbackToClientSideBlazor>(CreateAdditionalArgs(args)), "/fallback"),
            ["Multiple components (Server-side)"] = (BuildWebHost<MultipleComponents>(CreateAdditionalArgs(args)), "/multiple-components"),
            ["Save state"] = (BuildWebHost<SaveState>(CreateAdditionalArgs(args)), "/save-state"),
            ["Globalization + Localization (Server-side)"] = (BuildWebHost<InternationalizationStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["Server-side blazor"] = (BuildWebHost<ServerStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["Blazor web with server-side blazor root component"] = (BuildWebHost<RazorComponentEndpointsStartup<Root>>(CreateAdditionalArgs(args)), "/subdir"),
            ["Hosted client-side blazor"] = (BuildWebHost<ClientStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["Hot Reload"] = (BuildWebHost<HotReloadStartup>(CreateAdditionalArgs(args)), "/subdir"),
            ["Dev server client-side blazor"] = CreateDevServerHost(CreateAdditionalArgs(args)),
            ["Global Interactivity"] = (BuildWebHost<RazorComponentEndpointsStartup<GlobalInteractivityApp>>(CreateAdditionalArgs(args)), "/subdir"),
        };

        var mainHost = BuildWebHost(args);

        await Task.WhenAll(createIndividualHosts.Select(s => s.Value.host.StartAsync()));

        var testAppInfo = mainHost.Services.GetRequiredService<TestAppInfo>();
        testAppInfo.Scenarios = createIndividualHosts
            .ToDictionary(kvp => kvp.Key,
            kvp => kvp.Value.host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.FirstOrDefault()
                .Replace("127.0.0.1", "localhost") + kvp.Value.basePath);

        await mainHost.RunAsync();
    }

    private static (IHost host, string basePath) CreateDevServerHost(string[] args)
    {
        var contentRoot = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "Microsoft.AspNetCore.InternalTesting.BasicTestApp.ContentRoot")
            .Value;
        var finalArgs = new List<string>();
        finalArgs.AddRange(args);
        finalArgs.AddRange(
        [
            "--contentroot", contentRoot,
            "--pathbase", "/subdir",
            "--applicationpath", typeof(BasicTestApp.Program).Assembly.Location,
        ]);

        if (WebAssemblyTestHelper.MultithreadingIsEnabled())
        {
            finalArgs.Add("--apply-cop-headers");
        }

        var host = DevServerProgram.BuildWebHost(finalArgs.ToArray());
        return (host, "/subdir");
    }

    private static string[] CreateAdditionalArgs(string[] args) =>
        args.Concat(new[] { "--urls", $"http://127.0.0.1:{GetNextChildAppPortNumber()}" }).ToArray();

    public static IHost BuildWebHost(string[] args) => BuildWebHost<Startup>(args);

    public static IHost BuildWebHost<TStartup>(string[] args) where TStartup : class =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((ctx, lb) =>
            {
                TestSink sink = new TestSink();
                lb.AddProvider(new TestLoggerProvider(sink));
                lb.Services.Add(ServiceDescriptor.Singleton(sink));
            })
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.UseStartup<TStartup>();

                // We require this line because we run in Production environment
                // and static web assets are only on by default during development.
                webHostBuilder.UseStaticWebAssets();
            })
            .Build();

    private static int GetNextChildAppPortNumber()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("TESTSERVER_USE_DETERMINISTIC_PORTS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return nextPortNumber++;
        }
        else
        {
            // Let the OS assign an available port
            return 0;
        }
    }
}
