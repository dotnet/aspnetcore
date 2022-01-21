// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using ControllersFromServicesClassLibrary;
using ControllersFromServicesWebSite.Components;
using ControllersFromServicesWebSite.TagHelpers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace ControllersFromServicesWebSite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var builder = services
            .AddControllersWithViews()
            .ConfigureApplicationPartManager(manager => manager.ApplicationParts.Clear())
            .AddApplicationPart(typeof(TimeScheduleController).GetTypeInfo().Assembly)
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(new TypesPart(
                  typeof(AnotherController),
                  typeof(ComponentFromServicesViewComponent),
                  typeof(InServicesTagHelper)));

                foreach (var part in CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(Assembly.GetExecutingAssembly()))
                {
                    manager.ApplicationParts.Add(part);
                }
            })
            .AddControllersAsServices()
            .AddViewComponentsAsServices()
            .AddTagHelpersAsServices();

        services.AddTransient<QueryValueService>();
        services.AddTransient<ValueService>();
        services.AddHttpContextAccessor();
    }

    private class TypesPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public TypesPart(params Type[] types)
        {
            Types = types.Select(t => t.GetTypeInfo());
        }

        public override string Name => string.Join(", ", Types.Select(t => t.FullName));

        public IEnumerable<TypeInfo> Types { get; }
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }

    public static void Main(string[] args)
    {
        var host = CreateWebHostBuilder(args)
            .Build();

        host.Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        new WebHostBuilder()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseStartup<Startup>()
            .UseKestrel()
            .UseIISIntegration();
}

