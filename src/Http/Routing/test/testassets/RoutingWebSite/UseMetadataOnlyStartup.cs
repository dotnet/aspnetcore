// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RoutingWebSite;

public class UseMetadataOnlyStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<EndsWithStringRouteConstraint>();

        services.AddRouting(options =>
        {
            options.ConstraintMap.Add("endsWith", typeof(EndsWithStringRouteConstraint));
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();

        app.UseRouting();

        // Imagine some more stuff here...

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMetadata("/{**subpath}").WithMetadata(new { Whatever = "This is on every endpoint now!" });
            endpoints.MapGet("/printmeta", (HttpContext context) => context.GetEndpoint()?.Metadata
                .Select(m => new { TypeName = m.GetType().FullName, Value = m.ToString() }))
                .WithMetadata(new { Value = "This is only on this single endpoint" });
        });
    }
}
