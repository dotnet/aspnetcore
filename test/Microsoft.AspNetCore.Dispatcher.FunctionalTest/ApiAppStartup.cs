// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Dispatcher.FunctionalTest
{
    public class ApiAppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddDispatcher();

            // This is a temporary layering issue, don't worry about it :)
            services.AddRouting();
            services.AddSingleton<RouteTemplateUrlGenerator>();
            services.AddSingleton<IDefaultMatcherFactory, TreeMatcherFactory>();

            services.Configure<DispatcherOptions>(ConfigureDispatcher);
        }

        public void Configure(IApplicationBuilder app, ILogger<ApiAppStartup> logger)
        {
            app.UseDispatcher();

            app.Use(next => async (context) =>
            {
                logger.LogInformation("Executing fake CORS middleware");

                var feature = context.Features.Get<IDispatcherFeature>();
                var policy = feature.Endpoint?.Metadata.OfType<CorsPolicyMetadata>().LastOrDefault();
                logger.LogInformation("using CORS policy {PolicyName}", policy?.Name ?? "default");

                await next(context);
            });

            app.Use(next => async (context) =>
            {
                logger.LogInformation("Executing fake AuthZ middleware");

                var feature = context.Features.Get<IDispatcherFeature>();
                var policy = feature.Endpoint?.Metadata.OfType<AuthorizationPolicyMetadata>().LastOrDefault();
                if (policy != null)
                {
                    logger.LogInformation("using Auth policy {PolicyName}", policy.Name);
                }

                await next(context);
            });
        }

        public void ConfigureDispatcher(DispatcherOptions options)
        {
            options.Matchers.Add(new TreeMatcher()
            {
                Endpoints =
                {
                    new TemplateEndpoint("api/products", Products_Fallback),
                    new TemplateEndpoint("api/products", new { controller = "Products", action = "Get", }, "GET", Products_Get),
                    new TemplateEndpoint("api/products/{id}", new { controller = "Products", action = "Get", }, "GET", Products_GetWithId),
                    new TemplateEndpoint("api/products", new { controller = "Products", action = "Post", }, "POST", Products_Post),
                    new TemplateEndpoint("api/products/{id}", new { controller = "Products", action = "Put", }, "PUT", Products_Put),
                },

                Selectors =
                {
                    new HttpMethodEndpointSelector(),
                },
            }, new TemplateEndpointHandlerFactory());
        }

        private Task Products_Fallback(HttpContext httpContext) => httpContext.Response.WriteAsync("Hello, Products_Fallback");

        private Task Products_Get(HttpContext httpContext) => httpContext.Response.WriteAsync("Hello, Products_Get");

        private Task Products_GetWithId(HttpContext httpContext) => httpContext.Response.WriteAsync("Hello, Products_GetWithId");

        private Task Products_Post(HttpContext httpContext) => httpContext.Response.WriteAsync("Hello, Products_Post");

        private Task Products_Put(HttpContext httpContext) => httpContext.Response.WriteAsync("Hello, Products_Put");

        private class CorsPolicyMetadata
        {
            public string Name { get; set; }
        }

        private class AuthorizationPolicyMetadata
        {
            public string Name { get; set; }
        }
    }
}
