// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DevHostServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public class BlazorWasmTestAppFixture<TProgram> : WebHostServerFixture
    {
        public readonly bool TestTrimmedApps = false;
        //typeof(ToggleExecutionModeServerFixture<>).Assembly
        //    .GetCustomAttributes<AssemblyMetadataAttribute>()
        //    .First(m => m.Key == "Microsoft.AspNetCore.E2ETesting.TestTrimmedApps")
        //    .Value == "true";

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

            ContentRoot = FindSampleOrTestSitePath(
                typeof(TProgram).Assembly.FullName);

            var host = "127.0.0.1";

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
                app.UseBlazorFrameworkFiles();
                app.UseStaticFiles(new StaticFileOptions
                {
                    ServeUnknownFileTypes = true,
                });

                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    var fallback = "index.html";
                    if (!string.IsNullOrEmpty(PathBase))
                    {
                        fallback = PathBase + '/' + fallback;
                    }

                    endpoints.MapFallbackToFile(fallback);
                });
            }
        }
    }
}
