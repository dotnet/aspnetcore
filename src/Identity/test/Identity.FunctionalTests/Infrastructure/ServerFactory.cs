// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

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

    public string BootstrapFrameworkVersion { get; set; } = "V5";

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

        UpdateApplicationParts(builder);
    }

    private void UpdateApplicationParts(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => AddRelatedParts(services, BootstrapFrameworkVersion));

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
                [UIFramework.Bootstrap5] = "Microsoft.AspNetCore.Identity.UI.Views.V5",
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

                var selectedFrameworkAssembly = _assemblyMap[framework == "V4" ? UIFramework.Bootstrap4 : UIFramework.Bootstrap5];

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
