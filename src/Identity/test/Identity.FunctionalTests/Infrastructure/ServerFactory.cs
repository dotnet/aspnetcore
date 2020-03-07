// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class ServerFactory<TStartup, TContext> : WebApplicationFactory<TStartup>
        where TStartup : class
        where TContext : DbContext
    {
        private readonly SqliteConnection _connection
            = new SqliteConnection($"DataSource=:memory:");

        public ServerFactory()
        {
            _connection.Open();

            ClientOptions.AllowAutoRedirect = false;
            ClientOptions.BaseAddress = new Uri("https://localhost");
        }

        public string BootstrapFrameworkVersion { get; set; } = "V4";

        protected override IHostBuilder CreateHostBuilder()
        {
            return Program.CreateHostBuilder(new[] { "--use-startup=false" });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseStartup<TStartup>();

            builder.ConfigureServices(sc =>
            {
                sc.SetupTestDatabase<TContext>(_connection)
                    .AddMvc()
                    // Mark the cookie as essential for right now, as Identity uses it on
                    // several places to pass important data in post-redirect-get flows.
                    .AddCookieTempDataProvider(o => o.Cookie.IsEssential = true);
            });

            UpdateStaticAssets(builder);
            UpdateApplicationParts(builder);
        }

        private void UpdateApplicationParts(IWebHostBuilder builder) =>
            builder.ConfigureServices(services => AddRelatedParts(services, BootstrapFrameworkVersion));

        private void UpdateStaticAssets(IWebHostBuilder builder)
        {
            var manifestPath = Path.GetDirectoryName(new Uri(typeof(ServerFactory<,>).Assembly.CodeBase).LocalPath);
            builder.ConfigureAppConfiguration((ctx, cb) =>
            {
                if (ctx.HostingEnvironment.WebRootFileProvider is CompositeFileProvider composite)
                {
                    var originalWebRoot = composite.FileProviders.First();
                    ctx.HostingEnvironment.WebRootFileProvider = originalWebRoot;
                }
            });

            string versionedPath = Path.Combine(manifestPath, $"Testing.DefaultWebSite.StaticWebAssets.{BootstrapFrameworkVersion}.xml");
            UpdateManifest(versionedPath);

            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                using (var manifest = File.OpenRead(versionedPath))
                {
                    typeof(StaticWebAssetsLoader)
                        .GetMethod("UseStaticWebAssetsCore", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { context.HostingEnvironment, manifest });
                }
            });
        }

        private void UpdateManifest(string versionedPath)
        {
            var content = File.ReadAllText(versionedPath);
            var path = typeof(ServerFactory<,>).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Single(a => a.Key == "Microsoft.AspNetCore.Testing.IdentityUIProjectPath").Value;

            path = Directory.Exists(path) ? Path.Combine(path, "wwwroot") : Path.Combine(FindHelixSlnFileDirectory(), "UI", "wwwroot");

            var updatedContent = content.Replace("{TEST_PLACEHOLDER}", path);

            File.WriteAllText(versionedPath, updatedContent);
        }

        private string FindHelixSlnFileDirectory()
        {
            var applicationPath = Path.GetDirectoryName(typeof(ServerFactory<,>).Assembly.Location);
            var directoryInfo = new DirectoryInfo(applicationPath);
            do
            {
                var solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, "*.sln").FirstOrDefault();
                if (solutionPath != null)
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new InvalidOperationException($"Solution root could not be located using application root {applicationPath}.");
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var result = base.CreateHost(builder);
            EnsureDatabaseCreated(result.Services);
            return result;
        }

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            var result = base.CreateServer(builder);
            EnsureDatabaseCreated(result.Host.Services);
            return result;
        }

        public void EnsureDatabaseCreated(IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                scope.ServiceProvider.GetService<TContext>()?.Database?.EnsureCreated();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _connection.Dispose();

            base.Dispose(disposing);
        }

        private static void AddRelatedParts(IServiceCollection services, string framework)
        {
            var _assemblyMap =
                new Dictionary<UIFramework, string>()
                {
                    [UIFramework.Bootstrap3] = "Microsoft.AspNetCore.Identity.UI.Views.V3",
                    [UIFramework.Bootstrap4] = "Microsoft.AspNetCore.Identity.UI.Views.V4",
                };

            var mvcBuilder = services
                .AddMvc()
                .ConfigureApplicationPartManager(partManager =>
                {
                    var thisAssembly = typeof(IdentityBuilderUIExtensions).Assembly;
                    var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(thisAssembly, throwOnError: true);
                    var relatedParts = relatedAssemblies.ToDictionary(
                        ra => ra,
                        CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts);

                    var selectedFrameworkAssembly = _assemblyMap[framework == "V3" ? UIFramework.Bootstrap3 : UIFramework.Bootstrap4];

                    foreach (var kvp in relatedParts)
                    {
                        var assemblyName = kvp.Key.GetName().Name;
                        if (!IsAssemblyForFramework(selectedFrameworkAssembly, assemblyName))
                        {
                            RemoveParts(partManager, kvp.Value);
                        }
                        else
                        {
                            AddParts(partManager, kvp.Value);
                        }
                    }

                    bool IsAssemblyForFramework(string frameworkAssembly, string assemblyName) =>
                        string.Equals(assemblyName, frameworkAssembly, StringComparison.OrdinalIgnoreCase);

                    void RemoveParts(
                        ApplicationPartManager manager,
                        IEnumerable<ApplicationPart> partsToRemove)
                    {
                        for (var i = 0; i < manager.ApplicationParts.Count; i++)
                        {
                            var part = manager.ApplicationParts[i];
                            if (partsToRemove.Any(p => string.Equals(
                                    p.Name,
                                    part.Name,
                                    StringComparison.OrdinalIgnoreCase)))
                            {
                                manager.ApplicationParts.Remove(part);
                            }
                        }
                    }

                    void AddParts(
                        ApplicationPartManager manager,
                        IEnumerable<ApplicationPart> partsToAdd)
                    {
                        foreach (var part in partsToAdd)
                        {
                            if (!manager.ApplicationParts.Any(p => p.GetType() == part.GetType() &&
                                string.Equals(p.Name, part.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                manager.ApplicationParts.Add(part);
                            }
                        }
                    }
                });
        }

    }
}
