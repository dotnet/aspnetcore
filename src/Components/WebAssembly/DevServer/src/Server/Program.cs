// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server;

// This project is a CLI tool, so we don't expect anyone to reference it
// as a runtime library. As such we consider it reasonable to mark the
// following method as public purely so the E2E tests project can invoke it.

/// <summary>
/// Intended for framework test use only.
/// </summary>
public class Program
{
    /// <summary>
    /// Intended for framework test use only.
    /// </summary>
    public static IHost BuildWebHost(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(config =>
            {
                var applicationPath = args.SkipWhile(a => a != "--applicationpath").Skip(1).First();
                var applicationDirectory = Path.GetDirectoryName(applicationPath)!;
                var name = Path.ChangeExtension(applicationPath, ".staticwebassets.runtime.json");
                name = !File.Exists(name) ? Path.ChangeExtension(applicationPath, ".StaticWebAssets.xml") : name;

                var endpointsManifest = Path.ChangeExtension(applicationPath, ".staticwebassets.endpoints.json");

                var inMemoryConfiguration = new Dictionary<string, string?>
                {
                    [WebHostDefaults.EnvironmentKey] = "Development",
                    ["Logging:LogLevel:Microsoft"] = "Warning",
                    ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information",
                    [WebHostDefaults.StaticWebAssetsKey] = name,
                    ["staticAssets"] = endpointsManifest,
                    ["ApplyCopHeaders"] = args.Contains("--apply-cop-headers").ToString()
                };

                config.AddInMemoryCollection(inMemoryConfiguration);
                config.AddJsonFile(Path.Combine(applicationDirectory, "blazor-devserversettings.json"), optional: true, reloadOnChange: true);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStaticWebAssets();
                webBuilder.UseStartup<Startup>();
            }).Build();
}
