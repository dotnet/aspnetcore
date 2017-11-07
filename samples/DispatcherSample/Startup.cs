// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Dispatcher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DispatcherSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDispatcher();

            // This is a temporary layering issue, don't worry about :)
            services.AddRouting();
            services.AddSingleton<IDefaultMatcherFactory, TreeMatcherFactory>();

            // Imagine this was done by MVC or another framework.
            services.AddSingleton<DispatcherDataSource>(ConfigureDispatcher());
            services.AddSingleton<EndpointSelector, HttpMethodEndpointSelector>();

        }

        public DefaultDispatcherDataSource ConfigureDispatcher()
        {
            return new DefaultDispatcherDataSource()
            {
                Addresses =
                {
                    new RoutePatternAddress("{id?}", new { controller = "Home", action = "Index", }, "Home:Index()"),
                    new RoutePatternAddress("Home/About/{id?}", new { controller = "Home", action = "About", }, "Home:About()"),
                    new RoutePatternAddress("Admin/Index/{id?}", new { controller = "Admin", action = "Index", }, "Admin:Index()"),
                    new RoutePatternAddress("Admin/Users/{id?}", new { controller = "Admin", action = "Users", }, "Admin:GetUsers()/Admin:EditUsers()"),
                },
                Endpoints =
                {
                    new RoutePatternEndpoint("{id?}", new { controller = "Home", action = "Index", }, Home_Index, "Home:Index()"),
                    new RoutePatternEndpoint("Home/{id?}", new { controller = "Home", action = "Index", }, Home_Index, "Home:Index()"),
                    new RoutePatternEndpoint("Home/Index/{id?}", new { controller = "Home", action = "Index", }, Home_Index, "Home:Index()"),
                    new RoutePatternEndpoint("Home/About/{id?}", new { controller = "Home", action = "About", }, Home_About, "Home:About()"),
                    new RoutePatternEndpoint("Admin/Index/{id?}", new { controller = "Admin", action = "Index", }, Admin_Index, "Admin:Index()"),
                    new RoutePatternEndpoint("Admin/Users/{id?}", new { controller = "Admin", action = "Users", }, "GET", Admin_GetUsers, "Admin:GetUsers()", new AuthorizationPolicyMetadata("Admin")),
                    new RoutePatternEndpoint("Admin/Users/{id?}", new { controller = "Admin", action = "Users", }, "POST", Admin_EditUsers, "Admin:EditUsers()", new AuthorizationPolicyMetadata("Admin")),
                },
            };
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILogger<Startup> logger)
        {
            app.UseDispatcher();

            app.Use(async (context, next) =>
            {
                logger.LogInformation("Executing fake CORS middleware");

                var feature = context.Features.Get<IDispatcherFeature>();
                var policy = feature.Endpoint?.Metadata.GetMetadata<ICorsPolicyMetadata>();
                logger.LogInformation("using CORS policy {PolicyName}", policy?.Name ?? "default");

                await next.Invoke();
            });

            app.Use(async (context, next) =>
            {
                logger.LogInformation("Executing fake AuthZ middleware");

                var feature = context.Features.Get<IDispatcherFeature>();
                var policy = feature.Endpoint?.Metadata.GetMetadata<IAuthorizationPolicyMetadata>();
                if (policy != null)
                {
                    logger.LogInformation("using Auth policy {PolicyName}", policy.Name);
                }

                await next.Invoke();
            });
        }

        public static Task Home_Index(HttpContext httpContext)
        {
            var templateFactory = httpContext.RequestServices.GetRequiredService<TemplateFactory>();

            return httpContext.Response.WriteAsync(
                $"<html>" +
                $"<body>" +
                $"<h1>Some links you can visit</h1>" +
                $"<p><a href=\"{templateFactory.GetTemplate(new { controller = "Home", action = "Index", }).GetUrl(httpContext)}\">Home:Index()</a></p>" +
                $"<p><a href=\"{templateFactory.GetTemplate(new { controller = "Home", action = "About", }).GetUrl(httpContext)}\">Home:About()</a></p>" +
                $"<p><a href=\"{templateFactory.GetTemplate(new { controller = "Admin", action = "Index", }).GetUrl(httpContext)}\">Admin:Index()</a></p>" +
                $"<p><a href=\"{templateFactory.GetTemplate(new { controller = "Admin", action = "Users", }).GetUrl(httpContext)}\">Admin:GetUsers()/Admin:EditUsers()</a></p>" +
                $"</body>" +
                $"</html>");
        }

        public static Task Home_About(HttpContext httpContext)
        {
            return httpContext.Response.WriteAsync(
                $"<html>" +
                $"<body>" +
                $"<p>This is a dispatcher sample.</p>" +
                $"</body>" +
                $"</html>");
        }

        public static Task Admin_Index(HttpContext httpContext)
        {
            return httpContext.Response.WriteAsync(
                $"<html>" +
                $"<body>" +
                $"<p>This is the admin page.</p>" +
                $"</body>" +
                $"</html>");
        }

        public static Task Admin_GetUsers(HttpContext httpContext)
        {
            return httpContext.Response.WriteAsync(
                $"<html>" +
                $"<body>" +
                $"<p>Users: rynowak, jbagga</p>" +
                $"</body>" +
                $"</html>");
        }

        public static Task Admin_EditUsers(HttpContext httpContext)
        {
            return httpContext.Response.WriteAsync(
                $"<html>" +
                $"<body>" +
                $"<p>blerp</p>" +
                $"</body>" +
                $"</html>");
        }
    }
}
