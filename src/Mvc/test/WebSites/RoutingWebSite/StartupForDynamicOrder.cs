// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace RoutingWebSite;

// For by tests for dynamic routing to pages/controllers
public class StartupForDynamicOrder
{
    public static class DynamicOrderScenarios
    {
        public const string AttributeRouteDynamicRoute = nameof(AttributeRouteDynamicRoute);
        public const string MultipleDynamicRoute = nameof(MultipleDynamicRoute);
        public const string ConventionalRouteDynamicRoute = nameof(ConventionalRouteDynamicRoute);
        public const string DynamicControllerAndPages = nameof(DynamicControllerAndPages);
    }

    public IConfiguration Configuration { get; }

    public StartupForDynamicOrder(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMvc()
            .AddNewtonsoftJson();

        services.AddTransient<Transformer>();
        services.AddScoped<TestResponseGenerator>();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        // Used by some controllers defined in this project.
        services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
    }

    public void Configure(IApplicationBuilder app)
    {
        var scenario = Configuration.GetValue<string>("Scenario");
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            // Route order definition is important for all these routes:
            switch (scenario)
            {
                case DynamicOrderScenarios.AttributeRouteDynamicRoute:
                    endpoints.MapDynamicControllerRoute<Transformer>("attribute-dynamic-order/{**slug}", new TransformerState() { Identifier = "slug" });
                    endpoints.MapControllers();
                    break;
                case DynamicOrderScenarios.ConventionalRouteDynamicRoute:
                    endpoints.MapControllerRoute(null, "{**conventional-dynamic-order-before:regex(^((?!conventional\\-dynamic\\-order\\-after).)*$)}", new { controller = "DynamicOrder", action = "Index" });
                    endpoints.MapDynamicControllerRoute<Transformer>("{conventional-dynamic-order}", new TransformerState() { Identifier = "slug" });
                    endpoints.MapControllerRoute(null, "conventional-dynamic-order-after", new { controller = "DynamicOrder", action = "Index" });
                    break;
                case DynamicOrderScenarios.MultipleDynamicRoute:
                    endpoints.MapDynamicControllerRoute<Transformer>("dynamic-order/{**slug}", new TransformerState() { Identifier = "slug" });
                    endpoints.MapDynamicControllerRoute<Transformer>("dynamic-order/specific/{**slug}", new TransformerState() { Identifier = "specific" });
                    break;
                case DynamicOrderScenarios.DynamicControllerAndPages:
                    endpoints.MapDynamicPageRoute<Transformer>("{**dynamic-order-page-controller-before:regex(^((?!dynamic\\-order\\-page\\-controller\\-after).)*$)}", new TransformerState() { Identifier = "before", ForPages = true });
                    endpoints.MapDynamicControllerRoute<Transformer>("{dynamic-order-page-controller}", new TransformerState() { Identifier = "controller" });
                    endpoints.MapDynamicPageRoute<Transformer>("dynamic-order-page-controller-after", new TransformerState() { Identifier = "after", ForPages = true });
                    break;
                default:
                    throw new InvalidOperationException("Invalid scenario configuration.");
            }
        });

        app.Map("/afterrouting", b => b.Run(c =>
        {
            return c.Response.WriteAsync("Hello from middleware after routing");
        }));
    }

    private class TransformerState
    {
        public string Identifier { get; set; }
        public bool ForPages { get; set; }
    }

    private class Transformer : DynamicRouteValueTransformer
    {
        // Turns a format like `controller=Home,action=Index` into an RVD
        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            var kvps = ((string)values?["slug"])?.Split("/")?.LastOrDefault()?.Split(",") ?? Array.Empty<string>();

            // Go to index by default if the route doesn't follow the slug pattern, we want to make sure always match to
            // test the order is applied
            var state = (TransformerState)State;
            var results = new RouteValueDictionary();
            if (!state.ForPages)
            {
                results["controller"] = "Home";
                results["action"] = "Index";
            }
            else
            {
                results["Page"] = "/DynamicPage";
            }

            foreach (var kvp in kvps)
            {
                var split = kvp.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase).Split("=");
                if (split.Length == 2)
                {
                    results[split[0]] = split[1];
                }
            }

            results["identifier"] = ((TransformerState)State).Identifier;

            return new ValueTask<RouteValueDictionary>(results);
        }
    }
}
