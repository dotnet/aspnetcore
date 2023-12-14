// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace RoutingWebSite;

public class StartupForEndpointFilters
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc().AddNewtonsoftJson();

        // Used by some controllers defined in this project.
        services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
        services.AddScoped<TestResponseGenerator>();
        // This is used by test response generator
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(builder =>
        {
            builder.MapControllers().AddEndpointFilterFactory((context, next) =>
            {
                return async ic =>
                {
                    ic.HttpContext.Items[nameof(IEndpointFilter)] = true;
                    ic.HttpContext.Items[nameof(EndpointFilterFactoryContext.MethodInfo.Name)] = context.MethodInfo.Name;
                    var result = await next(ic);
                    if (context.MethodInfo.Name == "IndexWithSelectiveFilter")
                    {
                        return "Intercepted";
                    }
                    return result;
                };
            }).AddEndpointFilterFactory((context, next) =>
            {
                if (context.MethodInfo.GetParameters().Length >= 1 && context.MethodInfo.GetParameters()[0].ParameterType == typeof(string))
                {
                    return ic =>
                    {
                        var firstArg = ic.GetArgument<string>(0);
                        ic.HttpContext.Items[nameof(EndpointFilterInvocationContext.Arguments)] = firstArg;
                        return next(ic);
                    };
                }

                return ic => next(ic);
            });
        });
    }
}
