// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using BasicWebSite.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BasicWebSite;

public class StartupWithoutEndpointRouting
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new TestService { Message = "true" });

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Api", _ => { });
        services.AddTransient<IAuthorizationHandler, ManagerHandler>();

        services
            .AddMvc(options =>
            {
                options.Conventions.Add(new ApplicationDescription("This is a basic website."));
                // Filter that records a value in HttpContext.Items
                options.Filters.Add(new TraceResourceFilter());

                options.EnableEndpointRouting = false;
            })
            .AddNewtonsoftJson()
            .AddXmlDataContractSerializerFormatters();

        services.ConfigureBaseWebSiteAuthPolicies();

        services.AddTransient<IAuthorizationHandler, ManagerHandler>();

        services.AddLogging();
        services.AddSingleton<IActionDescriptorProvider, ActionDescriptorCreationCounter>();
        services.AddHttpContextAccessor();
        services.AddSingleton<ContactsRepository>();
        services.AddKeyedSingleton<ICustomService, OkCustomService>("ok_service");
        services.AddKeyedSingleton<ICustomService, BadCustomService>("not_ok_service");
        services.AddSingleton<ICustomService, DefaultCustomService>();
        services.AddScoped<RequestIdService>();
        services.AddTransient<ServiceActionFilter>();
        services.AddScoped<TestResponseGenerator>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.TryAddSingleton(CreateWeatherForecastService);
    }

    // For manual debug only (running this test site with F5)
    // This needs to be changed to match the site host
    private WeatherForecastService CreateWeatherForecastService(IServiceProvider serviceProvider)
    {
        var contextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = contextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("Needs a request context!");
        }
        var client = new HttpClient();
        client.BaseAddress = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}");
        return new WeatherForecastService(client);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        app.UseStaticFiles();

        // Initializes the RequestId service for each request
        app.UseMiddleware<RequestIdMiddleware>();

        // Add MVC to the request pipeline
        app.UseMvc(routes =>
        {
            routes.MapRoute(
                "areaRoute",
                "{area:exists}/{controller}/{action}",
                new { controller = "Home", action = "Index" });

            routes.MapRoute("ActionAsMethod", "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" });

            routes.MapRoute("PageRoute", "{controller}/{action}/{page}");
        });
    }
}
