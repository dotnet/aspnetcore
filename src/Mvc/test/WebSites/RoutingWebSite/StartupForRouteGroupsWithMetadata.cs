// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Mvc.RoutingWebSite.Controllers;
using Mvc.RoutingWebSite.Infrastructure;

namespace RoutingWebSite;

public class StartupForRouteGroupsWithMetadata
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        var builder = services.AddControllers();

        // Remove the default controller feature provider so we don't find all of the controllers
        // in this app, we do this because adding controllers to multple groups with the same name
        // does not work.
        var old = builder.PartManager.FeatureProviders.OfType<IApplicationFeatureProvider<ControllerFeature>>().FirstOrDefault();
        builder.PartManager.FeatureProviders.Remove(old);
        builder.PartManager.FeatureProviders.Add(
            new ManualControllerFeatureProvider(f =>
            {
                f.Controllers.Add(typeof(ItemsController).GetTypeInfo());
                f.Controllers.Add(typeof(ConventionalControllerWithMetadata).GetTypeInfo());
            }));
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(builder =>
        {
            // Map all controllers (defined in the 
            builder.MapControllers();

            builder.MapGroup("/group1")
                   .WithMetadata(new MetadataAttribute("A"))
                   .MapControllerRoute("route1", "/metadata", new
                   {
                       controller = nameof(ConventionalControllerWithMetadata),
                       action = nameof(ConventionalControllerWithMetadata.GetMetadata)
                   })
                   .WithMetadata(new MetadataAttribute("B"));
        });
    }
}

