// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite;

public class StartupWithCustomInvalidModelStateFactory
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Api", _ => { });

        services
            .AddMvc()
            .AddNewtonsoftJson();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var result = new BadRequestObjectResult(context.ModelState);
                result.ContentTypes.Clear();
                result.ContentTypes.Add("application/vnd.error+json");

                return result;
            };
        });

        services.ConfigureBaseWebSiteAuthPolicies();

        services.AddLogging();
        services.AddSingleton<ContactsRepository>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
